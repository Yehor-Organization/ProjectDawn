using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ProjectDawnApi.Src.Hubs;

public static class HubExtensions
{
    public static int GetPlayerId(this Hub hub)
    {
        var claim = hub.Context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (claim == null)
            throw new HubException("Unauthenticated connection");

        return int.Parse(claim.Value);
    }
}