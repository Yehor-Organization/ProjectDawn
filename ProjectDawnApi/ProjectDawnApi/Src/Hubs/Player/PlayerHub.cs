using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi.Src.Services.Player;

namespace ProjectDawnApi;

public class PlayerHub : Hub
{
    private readonly PlayerInventoryService inventory;

    public PlayerHub(PlayerInventoryService inventory)
    {
        this.inventory = inventory;
    }

    // 🔁 renamed from RegisterPlayer → Connect
    public Task Connect(int playerId)
        => inventory.ConnectAsync(this, playerId);

    public override Task OnDisconnectedAsync(Exception exception)
        => inventory.HandleDisconnectAsync(this, exception);
}
