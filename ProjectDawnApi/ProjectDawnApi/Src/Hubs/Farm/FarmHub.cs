using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi;
using ProjectDawnApi.Src.Services.Farm;

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

    public Task JoinFarm(string farmId, int playerId)
        => sessions.JoinAsync(this, farmId, playerId);

    public Task LeaveFarm(string farmId, int playerId)
        => sessions.LeaveAsync(this, farmId, playerId);

    public override Task OnDisconnectedAsync(Exception ex)
        => sessions.HandleDisconnectAsync(this, ex);

    public Task UpdatePlayerTransformation(string farmId, int playerId, TransformationDM transform)
        => movement.UpdateAsync(this, farmId, playerId, transform);
}