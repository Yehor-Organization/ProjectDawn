using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;

public class PlayerInventoryService
{
    private readonly IHubContext<PlayerHub> hub;
    private readonly PlayerInventoryDBCommunicator db;
    private readonly ILogger<PlayerInventoryService> logger;

    public PlayerInventoryService(
        IHubContext<PlayerHub> hub,
        PlayerInventoryDBCommunicator db,
        ILogger<PlayerInventoryService> logger)
    {
        this.hub = hub;
        this.db = db;
        this.logger = logger;
    }

    public async Task ConnectAsync(Hub hub, int playerId)
    {
        var newConnId = hub.Context.ConnectionId;

        if (PlayerConnectionRegistry.ActiveConnections.TryGetValue(playerId, out var oldConnId)
            && oldConnId != newConnId)
        {
            await hub.Clients.Client(oldConnId)
                .SendAsync("Kicked", "Logged in from another device");

            await hub.Groups.RemoveFromGroupAsync(
                oldConnId,
                GetPlayerGroup(playerId));
        }

        PlayerConnectionRegistry.ActiveConnections[playerId] = newConnId;

        await hub.Groups.AddToGroupAsync(
            newConnId,
            GetPlayerGroup(playerId));
    }


    public Task HandleDisconnectAsync(Hub hub, Exception ex)
    {
        var connId = hub.Context.ConnectionId;

        var entry = PlayerConnectionRegistry.ActiveConnections
            .FirstOrDefault(x => x.Value == connId);

        if (!entry.Equals(default(KeyValuePair<int, string>)))
            PlayerConnectionRegistry.ActiveConnections.TryRemove(entry.Key, out _);

        return Task.CompletedTask;
    }

    // 🔔 CALLED BY API
    public async Task NotifyInventoryUpdatedAsync(int playerId, object payload)
    {
        await hub.Clients
            .Group(GetPlayerGroup(playerId))
            .SendAsync("InventoryUpdated", payload);
    }

    public static string GetPlayerGroup(int playerId)
        => $"player:{playerId}";
}