using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectDawnApi;

[Authorize]
public class FarmHub : Hub
{
    private readonly IHubContext<FarmListHub> farmListHub;
    private readonly PlayerTransformationService movement;
    private readonly FarmSessionService sessions;
    // 👈 ADD THIS

    public FarmHub(
        FarmSessionService sessions,
        PlayerTransformationService movement,
        IHubContext<FarmListHub> farmListHub) // 👈 INJECT
    {
        this.sessions = sessions;
        this.movement = movement;
        this.farmListHub = farmListHub;
    }

    // =======================
    // Farm session
    // =======================

    public async Task JoinFarm(string farmId)
    {
        int playerId = this.GetPlayerId();

        await sessions.JoinAsync(this, farmId, playerId);
        await farmListHub.Clients.All.SendAsync("FarmListUpdated");
    }

    public async Task LeaveFarm(string farmId)
    {
        int playerId = this.GetPlayerId();

        await sessions.LeaveAsync(this, farmId, playerId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, farmId);

        await Clients.Group(farmId).SendAsync(
            "PlayerLeft",
            playerId
        );
        await farmListHub.Clients.All.SendAsync("FarmListUpdated");

        // 🔔 BROADCAST farm list update
        Console.WriteLine("📤 [SERVER] FarmListUpdated sent (LeaveFarm)");
    }

    public override Task OnDisconnectedAsync(Exception ex)
        => sessions.HandleDisconnectAsync(this, ex);

    // =======================
    // Movement sync
    // =======================

    public Task UpdatePlayerTransformation(string farmId, TransformationDM transform)
    {
        int playerId = this.GetPlayerId();
        return movement.UpdateAsync(this, farmId, playerId, transform);
    }
}