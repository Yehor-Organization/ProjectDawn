using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ProjectDawnApi
{
    public class FarmHub : Hub
    {
        private readonly ProjectDawnDbContext _context;
        private readonly ILogger<FarmHub> _logger;

        public FarmHub(ProjectDawnDbContext context, ILogger<FarmHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task JoinFarm(string farmIdStr, int playerId)
        {
            _logger.LogInformation($"Player {playerId} attempting to join farm {farmIdStr} (ConnectionId: {Context.ConnectionId})");

            if (!int.TryParse(farmIdStr, out int farmId))
            {
                _logger.LogWarning($"Invalid farmId: {farmIdStr}");
                return;
            }

            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                _logger.LogWarning($"Player {playerId} does not exist.");
                return;
            }

            // Add this connection to the SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, farmIdStr);

            // ✅ First update/add visitor in DB
            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId);

            if (visitor == null)
            {
                visitor = new FarmVisitorDataModel
                {
                    FarmId = farmId,
                    PlayerId = playerId,
                    ConnectionId = Context.ConnectionId
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

            // ✅ Now send existing players (excluding yourself)
            var existingVisitors = await _context.FarmVisitors
                .Where(v => v.FarmId == farmId && v.PlayerId != playerId)
                .Select(v => v.PlayerId)
                .ToListAsync();

            await Clients.Caller.SendAsync("InitialPlayers", existingVisitors);

            // Notify others about the new player
            await Clients.OthersInGroup(farmIdStr).SendAsync("PlayerJoined", playerId);
        }


        public async Task UpdatePlayerPosition(string farmIdStr, int playerId, float x, float y, float z)
        {
            await Clients.OthersInGroup(farmIdStr)
                .SendAsync("PlayerPositionUpdated", playerId, new { x, y, z });

            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == int.Parse(farmIdStr) && v.PlayerId == playerId);

            if (visitor != null)
            {
                visitor.LastPositionX = x;
                visitor.LastPositionY = y;
                visitor.LastPositionZ = z;
                await _context.SaveChangesAsync();
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var visitor = await _context.FarmVisitors
                .FirstOrDefaultAsync(v => v.ConnectionId == Context.ConnectionId);

            if (visitor != null)
            {
                await Clients.OthersInGroup(visitor.FarmId.ToString())
                    .SendAsync("PlayerLeft", visitor.PlayerId);

                _context.FarmVisitors.Remove(visitor);
                await _context.SaveChangesAsync();
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}
