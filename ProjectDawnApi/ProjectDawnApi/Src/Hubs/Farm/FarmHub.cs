using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi;

[Authorize]
public class FarmHub : Hub
{
    private readonly PlayerTransformationService movement;
    private readonly FarmSessionService sessions;

    public FarmHub(
        FarmSessionService sessions,
        PlayerTransformationService movement)
    {
        this.sessions = sessions;
        this.movement = movement;
    }

    // =======================
    // Farm session
    // =======================

    public async Task JoinFarm(string farmId)
    {
        int playerId = this.GetPlayerId();

        await sessions.JoinAsync(this, farmId, playerId);
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