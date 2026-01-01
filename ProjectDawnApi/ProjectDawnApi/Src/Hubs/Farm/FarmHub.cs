using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi;
using ProjectDawnApi.Src.Hubs;
using ProjectDawnApi.Src.Services.Farm;

[Authorize]
public class FarmHub : Hub
{
    private readonly PlayerTransformationService movement;
    private readonly FarmObjectService objects;
    private readonly FarmSessionService sessions;

    public FarmHub(
        FarmSessionService sessions,
        PlayerTransformationService movement,
        FarmObjectService objects)
    {
        this.sessions = sessions;
        this.movement = movement;
        this.objects = objects;
    }

    public Task JoinFarm(string farmId)
    {
        int playerId = this.GetPlayerId();
        return sessions.JoinAsync(this, farmId, playerId);
    }

    public Task LeaveFarm(string farmId)
    {
        int playerId = this.GetPlayerId();
        return sessions.LeaveAsync(this, farmId, playerId);
    }

    public override Task OnDisconnectedAsync(Exception ex)
        => sessions.HandleDisconnectAsync(this, ex);

    public Task UpdatePlayerTransformation(string farmId, TransformationDM transform)
    {
        int playerId = this.GetPlayerId();
        return movement.UpdateAsync(this, farmId, playerId, transform);
    }
}