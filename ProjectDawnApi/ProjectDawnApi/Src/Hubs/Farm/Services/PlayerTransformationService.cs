using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi
{
    public class PlayerTransformationService
    {
        private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(60);
        private static readonly Dictionary<int, DateTime> LastSave = new();

        private readonly IServiceScopeFactory scopeFactory;

        public PlayerTransformationService(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        public async Task UpdateAsync(
            Hub hub,
            string farmIdStr,
            int playerId,
            TransformationDM transform)
        {
            await hub.Clients.OthersInGroup(farmIdStr)
                .SendAsync("PlayerTransformationUpdated", playerId, transform);

            if (!int.TryParse(farmIdStr, out int farmId))
                return;

            var now = DateTime.UtcNow;
            if (LastSave.TryGetValue(playerId, out var last) && now - last < SaveInterval)
                return;

            LastSave[playerId] = now;

            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ProjectDawnDbContext>();

            var visitor = await db.FarmVisitors
                .FirstOrDefaultAsync(v => v.FarmId == farmId && v.PlayerId == playerId);

            if (visitor != null)
            {
                visitor.Transformation = transform;
                await db.SaveChangesAsync();
            }
        }
    }

}
