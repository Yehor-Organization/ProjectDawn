using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;

[Authorize]
public class PlayerHub : Hub
{
    private readonly PlayerInventoryService inventory;

    public PlayerHub(PlayerInventoryService inventory)
    {
        this.inventory = inventory;
    }

    // 🔁 Connect without playerId
    public Task Connect()
    {
        int playerId = this.GetPlayerId();
        return inventory.ConnectAsync(this, playerId);
    }

    public override Task OnDisconnectedAsync(Exception exception)
        => inventory.HandleDisconnectAsync(this, exception);
}