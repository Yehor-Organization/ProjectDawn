using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection; // for IServiceScopeFactory
using System.Collections.Generic;
using System.Linq;

namespace ProjectDawnApi
{
    public class FarmHub : Hub
    {
        private readonly ProjectDawnDbContext _context;
        private readonly ILogger<FarmHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // Throttle saves per player
        private static readonly TimeSpan TransformationSaveInterval = TimeSpan.FromSeconds(5);
        private static readonly Dictionary<int, DateTime> LastTransformationSave = new();

        public FarmHub(ProjectDawnDbContext context, ILogger<FarmHub> logger, IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        public async Task JoinFarm(string farmIdStr, int playerId)
        {
            _logger.LogInformation($"Player {playerId} attempting to join farm {farmIdStr} (ConnectionId: {Context.ConnectionId})");

            if (!int.TryParse(farmIdStr, out int farmId))
            {
                _logger.LogWarning($"Invalid farmId: {farmIdStr}");
                await Clients.Caller.SendAsync("JoinFarmFailed", "Invalid farm ID.");
                return;
            }

            var player = await _context.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId);

            if (player == null)
            {
                _logger.LogWarning($"Player {playerId} does not exist.");
                await Clients.Caller.SendAsync("JoinFarmFailed", "Player does not exist.");
                Context.Abort();
                return;
            }

            // Check for an existing session for this player
            var existingVisitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.PlayerId == playerId);

            if (existingVisitor != null)
            {
                _logger.LogInformation($"Replacing old session for player {playerId} (old ConnId {existingVisitor.ConnectionId})");

                // Kick the old client
                await Clients.Client(existingVisitor.ConnectionId)
                    .SendAsync("Kicked", "You have been logged out because you logged in elsewhere.");

                // Remove from group and DB
                await Groups.RemoveFromGroupAsync(existingVisitor.ConnectionId, existingVisitor.FarmId.ToString());
                _context.FarmVisitors.Remove(existingVisitor);
                await _context.SaveChangesAsync();

                // Notify others
                await Clients.OthersInGroup(existingVisitor.FarmId.ToString())
                    .SendAsync("PlayerLeft", existingVisitor.PlayerId);
            }

            // Always continue with new session
            await Groups.AddToGroupAsync(Context.ConnectionId, farmIdStr);

            var visitor = new FarmVisitorDataModel
            {
                FarmId = farmId,
                PlayerId = playerId,
                ConnectionId = Context.ConnectionId,
                Transformation = new TransformationDataModel()
            };

            _context.FarmVisitors.Add(visitor);
            await _context.SaveChangesAsync();

            // Send list of current players back to the new client
            var existingVisitors = await _context.FarmVisitors
                .AsNoTracking()
                .Where(v => v.FarmId == farmId && v.PlayerId != playerId)
                .Select(v => v.PlayerId)
                .ToListAsync();

            await Clients.Caller.SendAsync("InitialPlayers", existingVisitors);
            await Clients.OthersInGroup(farmIdStr).SendAsync("PlayerJoined", playerId);

            _logger.LogInformation($"Player {playerId} successfully joined farm {farmId} (ConnId {Context.ConnectionId}).");
        }

        /// <summary>
        /// Broadcasts an object placement to all other clients in the farm.
        /// </summary>
        public async Task PlaceObject(string farmIdStr, int playerId, string typeKey, TransformationDataModel transformData)
        {
            if (!int.TryParse(farmIdStr, out int farmId))
                return;

            var newObjectId = Guid.NewGuid();

            _logger.LogInformation($"Player {playerId} placed object {newObjectId} of type {typeKey} in farm {farmId}");

            // Broadcast to everyone in the farm (including sender)
            await Clients.Group(farmIdStr)
                .SendAsync("ObjectPlaced", newObjectId, typeKey, transformData);

            // Save to DB
            var placement = new PlacedObjectDataModel
            {
                FarmId = farmId,
                ObjectId = newObjectId,
                Type = typeKey,
                Transformation = transformData
            };

            _context.PlacedObjects.Add(placement);
            await _context.SaveChangesAsync();
        }


        /// <summary>
        /// Explicit leave call from client (graceful exit).
        /// </summary>
        public async Task LeaveFarm(string farmIdStr, int playerId)
        {
            _logger.LogInformation($"Player {playerId} leaving farm {farmIdStr} (ConnectionId: {Context.ConnectionId})");

            if (!int.TryParse(farmIdStr, out int farmId))
                return;

            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId);

            if (visitor != null)
            {
                _context.FarmVisitors.Remove(visitor);
                await _context.SaveChangesAsync();

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, farmIdStr);
                await Clients.OthersInGroup(farmIdStr).SendAsync("PlayerLeft", playerId);

                _logger.LogInformation($"Player {playerId} removed from farm {farmIdStr}.");
            }
        }

        /// <summary>
        /// Updates both position and rotation of a player.
        /// </summary>
        public async Task UpdatePlayerTransformation(string farmIdStr, int playerId, TransformationDataModel transformation)
        {
            await Clients.OthersInGroup(farmIdStr)
                .SendAsync("PlayerTransformationUpdated", playerId, transformation);

            if (!int.TryParse(farmIdStr, out int farmId)) return;

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProjectDawnDbContext>();

            var visitor = await db.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId);

            if (visitor != null)
            {
                visitor.Transformation = transformation;
                await db.SaveChangesAsync();
            }
        }


        /// <summary>
        /// Always runs when the connection is lost (app crash, kill, or network drop).
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Connection {Context.ConnectionId} disconnected. Reason: {exception?.Message}");

            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.ConnectionId == Context.ConnectionId);

            if (visitor != null)
            {
                _context.FarmVisitors.Remove(visitor);
                await _context.SaveChangesAsync();

                await Clients.OthersInGroup(visitor.FarmId.ToString())
                    .SendAsync("PlayerLeft", visitor.PlayerId);

                _logger.LogInformation($"Player {visitor.PlayerId} auto-removed from farm {visitor.FarmId} on disconnect.");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
