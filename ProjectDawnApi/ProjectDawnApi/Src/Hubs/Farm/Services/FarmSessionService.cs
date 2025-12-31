using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi
{
    public class FarmSessionService
    {
        private readonly FarmSessionDBCommunicator dbCommunicator;
        private readonly ILogger<FarmSessionService> logger;

        public FarmSessionService(FarmSessionDBCommunicator dbCommunicator, ILogger<FarmSessionService> logger)
        {
            this.dbCommunicator = dbCommunicator;
            this.logger = logger;
        }

        public async Task JoinAsync(Hub hub, string farmIdStr, int playerId)
        {
            if (!int.TryParse(farmIdStr, out int farmId))
            {
                await hub.Clients.Caller.SendAsync("JoinFarmFailed", "Invalid farm ID");
                return;
            }

            await dbCommunicator.ReplaceExistingSessionAsync(hub, farmId, playerId);
            await hub.Groups.AddToGroupAsync(hub.Context.ConnectionId, farmIdStr);

            await dbCommunicator.AddVisitorAsync(farmId, playerId, hub.Context.ConnectionId);

            var others = await dbCommunicator.GetOtherPlayersAsync(farmId, playerId);
            await hub.Clients.Caller.SendAsync("InitialPlayers", others);
            await hub.Clients.OthersInGroup(farmIdStr).SendAsync("PlayerJoined", playerId);
        }

        public async Task LeaveAsync(Hub hub, string farmIdStr, int playerId)
        {
            if (!int.TryParse(farmIdStr, out int farmId))
                return;

            await dbCommunicator.RemoveVisitorAsync(farmId, playerId);
            await hub.Groups.RemoveFromGroupAsync(hub.Context.ConnectionId, farmIdStr);
            await hub.Clients.OthersInGroup(farmIdStr).SendAsync("PlayerLeft", playerId);
        }

        public async Task HandleDisconnectAsync(Hub hub, Exception ex)
        {
            var visitor = await dbCommunicator.RemoveByConnectionAsync(hub.Context.ConnectionId);
            if (visitor != null)
            {
                await hub.Clients.OthersInGroup(visitor.FarmId.ToString())
                    .SendAsync("PlayerLeft", visitor.PlayerId);
            }
        }
    }

}
