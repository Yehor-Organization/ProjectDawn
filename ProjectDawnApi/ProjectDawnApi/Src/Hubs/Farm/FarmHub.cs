using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi;

public class FarmHub : Hub
{
    private readonly FarmSessionService sessions;
    private readonly PlayerTransformationService movement;
    private readonly FarmObjectService objects;

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

    public Task PlaceObject(string farmId, int playerId, string type, TransformationDM transform)
        => objects.PlaceAsync(this, farmId, playerId, type, transform);

    public Task UpdatePlayerTransformation(string farmId, int playerId, TransformationDM transform)
        => movement.UpdateAsync(this, farmId, playerId, transform);

    public override Task OnDisconnectedAsync(Exception ex)
        => sessions.HandleDisconnectAsync(this, ex);
}
