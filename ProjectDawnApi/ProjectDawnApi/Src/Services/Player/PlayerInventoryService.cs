using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class PlayerInventoryService
{
    private readonly PlayerInventoryDBCommunicator db;
    private readonly IHubContext<PlayerHub> hub;
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

    // ---------------------------------
    // SIGNALR CONNECTION MANAGEMENT
    // ---------------------------------

    public static string GetPlayerGroup(int playerId)
        => $"player:{playerId}";

    public async Task AddItemAsync(
        int playerId,
        string itemType,
        int quantity)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            throw new ArgumentException("ItemType is required.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be positive.");

        var inventory = await db.GetInventoryWithItemsAsync(playerId)
            ?? throw new KeyNotFoundException("Inventory not found.");

        var item = inventory.Items
            .FirstOrDefault(i => i.ItemType == itemType);

        if (item != null)
        {
            item.Quantity += quantity;
        }
        else
        {
            inventory.Items.Add(new InventoryItemDM
            {
                ItemType = itemType,
                Quantity = quantity
            });
        }

        await db.SaveChangesAsync();

        // 🔔 notify client
        await NotifyInventoryUpdatedAsync(playerId, new
        {
            itemType,
            delta = quantity
        });

        logger.LogInformation(
            "Added {Qty} {Item} to player {PlayerId}",
            quantity, itemType, playerId);
    }

    public async Task ConnectAsync(Hub hubCtx, int playerId)
    {
        var newConnId = hubCtx.Context.ConnectionId;

        if (PlayerConnectionRegistry.ActiveConnections
                .TryGetValue(playerId, out var oldConnId)
            && oldConnId != newConnId)
        {
            await hubCtx.Clients.Client(oldConnId)
                .SendAsync("Kicked", "Logged in from another device");

            await hubCtx.Groups.RemoveFromGroupAsync(
                oldConnId,
                GetPlayerGroup(playerId));
        }

        PlayerConnectionRegistry.ActiveConnections[playerId] = newConnId;

        await hubCtx.Groups.AddToGroupAsync(
            newConnId,
            GetPlayerGroup(playerId));
    }

    // ---------------------------------
    // INVENTORY DOMAIN LOGIC (NEW)
    // ---------------------------------
    public async Task<object> GetInventoryAsync(int playerId)
    {
        var inventory = await db.GetInventoryWithItemsAsync(playerId)
            ?? throw new KeyNotFoundException("Inventory not found.");

        return new
        {
            inventory.PlayerId,
            Items = inventory.Items.Select(i => new
            {
                i.ItemType,
                i.Quantity
            })
        };
    }

    public Task HandleDisconnectAsync(Hub hubCtx, Exception? ex)
    {
        var connId = hubCtx.Context.ConnectionId;

        var entry = PlayerConnectionRegistry.ActiveConnections
            .FirstOrDefault(x => x.Value == connId);

        if (!entry.Equals(default(KeyValuePair<int, string>)))
            PlayerConnectionRegistry.ActiveConnections
                .TryRemove(entry.Key, out _);

        return Task.CompletedTask;
    }

    // ---------------------------------
    // SIGNALR NOTIFICATION
    // ---------------------------------

    public async Task NotifyInventoryUpdatedAsync(int playerId, object payload)
    {
        await hub.Clients
            .Group(GetPlayerGroup(playerId))
            .SendAsync("InventoryUpdated", payload);
    }
}