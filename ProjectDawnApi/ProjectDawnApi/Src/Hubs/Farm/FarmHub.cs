using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi;
using ProjectDawnApi.Src.Hubs;
using ProjectDawnApi.Src.Services.Farm;
using System.Threading;

[Authorize]
public class FarmHub : Hub
{
    private readonly PlayerTransformationService movement;
    private readonly FarmObjectService objects;
    private readonly FarmSessionService sessions;
    private readonly FarmQueryService farms;

    public FarmHub(
        FarmSessionService sessions,
        PlayerTransformationService movement,
        FarmObjectService objects,
        FarmQueryService farms)
    {
        this.sessions = sessions;
        this.movement = movement;
        this.objects = objects;
        this.farms = farms;
    }

    // =======================
    // Farm session
    // =======================

    public async Task JoinFarm(string farmId)
    {
        int playerId = this.GetPlayerId();

        await sessions.JoinAsync(this, farmId, playerId);

        // 🔔 Notify everyone the list changed
        await Clients.All.SendAsync(
            "FarmListUpdated",
            CancellationToken.None
        );

        // 🔔 Optional: incremental update
        if (await farms.GetFarmInfoAsync(farmId) is FarmInfoDTO farm)
        {
            await Clients.All.SendAsync(
                "FarmJoined",
                farm,
                CancellationToken.None
            );
        }
    }

    public async Task LeaveFarm(string farmId)
    {
        int playerId = this.GetPlayerId();

        await sessions.LeaveAsync(this, farmId, playerId);

        // 🔔 Notify everyone the list changed
        await Clients.All.SendAsync(
            "FarmListUpdated",
            CancellationToken.None
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