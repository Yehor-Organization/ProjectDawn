using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;

public class FarmSessionService
{
    private readonly FarmSessionDBCommunicator dbCommunicator;
    private readonly IHubContext<FarmListHub> farmListHub;
    private readonly ILogger<FarmSessionService> logger;
    private readonly PlayerTransformationService transformService;

    public FarmSessionService(
        FarmSessionDBCommunicator dbCommunicator,
        PlayerTransformationService transformService,
        IHubContext<FarmListHub> farmListHub, // 👈 ADD
        ILogger<FarmSessionService> logger)
    {
        this.dbCommunicator = dbCommunicator;
        this.transformService = transformService;
        this.farmListHub = farmListHub; // 👈 ADD
        this.logger = logger;
    }

    // -----------------------------
    // 🔑 API SUPPORT: Resolve farm for player
    // -----------------------------
    public async Task<int?> GetCurrentFarmForPlayerAsync(int playerId)
    {
        return await dbCommunicator
            .GetCurrentFarmForPlayerAsync(playerId);
    }

    // -----------------------------
    // HANDLE DISCONNECT (SignalR)
    // -----------------------------
    public async Task HandleDisconnectAsync(Hub hub, Exception? ex)
    {
        var visitor = await dbCommunicator
            .GetVisitorByConnectionAsync(hub.Context.ConnectionId);

        if (visitor == null)
            return;

        await CleanupSessionAsync(
            hub,
            visitor.FarmId,
            visitor.PlayerId);

        logger.LogInformation(
            "Player {PlayerId} disconnected from farm {FarmId}",
            visitor.PlayerId,
            visitor.FarmId);
    }

    // -----------------------------
    // JOIN FARM (SignalR)
    // -----------------------------
    public async Task JoinAsync(Hub hub, string farmIdStr, int playerId)
    {
        if (!int.TryParse(farmIdStr, out int farmId))
        {
            await hub.Clients.Caller
                .SendAsync("JoinFarmFailed", "Invalid farm ID");
            return;
        }

        // Ensure player is not in another farm
        await dbCommunicator.ReplaceExistingSessionAsync(
            hub, farmId, playerId);

        await hub.Groups.AddToGroupAsync(
            hub.Context.ConnectionId, farmIdStr);

        await dbCommunicator.AddVisitorAsync(
            farmId, playerId, hub.Context.ConnectionId);

        // Get list of other player IDs
        var others = await dbCommunicator
            .GetOtherPlayersAsync(farmId, playerId);

        // Send player IDs first
        await hub.Clients.Caller
            .SendAsync("InitialPlayers", others);

        // 🔑 NEW: Send current positions of existing players
        var playerStates = transformService.GetAllTransformsForFarm(farmId, playerId);

        if (playerStates.Count > 0)
        {
            await hub.Clients.Caller
                .SendAsync("InitialPlayerStates", playerStates);

            logger.LogInformation(
                "Sent {Count} initial player states to Player {PlayerId} in Farm {FarmId}",
                playerStates.Count, playerId, farmId);
        }

        // Notify others about new player
        await hub.Clients.OthersInGroup(farmIdStr)
            .SendAsync("PlayerJoined", playerId);

        logger.LogInformation(
            "Player {PlayerId} joined farm {FarmId}",
            playerId, farmId);
    }

    // -----------------------------
    // LEAVE FARM (SignalR)
    // -----------------------------
    public async Task LeaveAsync(Hub hub, string farmIdStr, int playerId)
    {
        if (!int.TryParse(farmIdStr, out int farmId))
            return;

        await CleanupSessionAsync(hub, farmId, playerId);

        logger.LogInformation(
            "Player {PlayerId} left farm {FarmId}",
            playerId, farmId);
    }

    private async Task CleanupSessionAsync(
            Hub hub,
        int farmId,
        int playerId)
    {
        string farmGroup = farmId.ToString();

        // Remove transform cache
        transformService.RemovePlayerTransform(farmId, playerId);

        // Remove DB session
        await dbCommunicator.RemoveVisitorAsync(farmId, playerId);

        // Remove SignalR group
        await hub.Groups.RemoveFromGroupAsync(
            hub.Context.ConnectionId,
            farmGroup);

        // Notify farm members
        await hub.Clients.OthersInGroup(farmGroup)
            .SendAsync("PlayerLeft", playerId);

        // 🔔 BROADCAST FARM LIST UPDATE (CRITICAL)
        await farmListHub.Clients.All
            .SendAsync("FarmListUpdated");
    }
}