using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class FarmListHub : Hub
{
    // =======================
    // Connection lifecycle
    // =======================

    public override Task OnConnectedAsync()
    {
        // Optional: track connected menu clients later
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception exception)
    {
        // Optional: cleanup / presence tracking later
        return base.OnDisconnectedAsync(exception);
    }
}