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
                return;
            }

            // Only proceed if player exists
            await Groups.AddToGroupAsync(Context.ConnectionId, farmIdStr);

            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId);

            if (visitor == null)
            {
                visitor = new FarmVisitorDataModel
                {
                    FarmId = farmId,
                    PlayerId = playerId,
                    ConnectionId = Context.ConnectionId,
                    Transformation = new TransformationDataModel()
                };
                _context.FarmVisitors.Add(visitor);
                _logger.LogInformation("Visitor added: {@Visitor}", visitor);
            }
            else
            {
                visitor.ConnectionId = Context.ConnectionId;
                _logger.LogInformation("Visitor updated ConnectionId: {@Visitor}", visitor);
            }

            await _context.SaveChangesAsync();

            var existingVisitors = await _context.FarmVisitors
                .AsNoTracking()
                .Where(v => v.FarmId == farmId && v.PlayerId != playerId)
                .Select(v => v.PlayerId)
                .ToListAsync();

            await Clients.Caller.SendAsync("InitialPlayers", existingVisitors);

            await Clients.OthersInGroup(farmIdStr).SendAsync("PlayerJoined", playerId);
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

            if (int.TryParse(farmIdStr, out int farmId))
            {
                // Store in dictionary (overwrite old value)
                TransformationQueue.Queue[(farmId, playerId)] = transformation;
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
