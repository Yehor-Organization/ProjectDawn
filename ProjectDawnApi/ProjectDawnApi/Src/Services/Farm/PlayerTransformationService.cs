using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;

public class PlayerTransformationService
{
    private static readonly Dictionary<int, DateTime> LastSave = new();
    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(60);

    private readonly PlayerTransformationDBCommunicator dbCommunicator;

    public PlayerTransformationService(
        PlayerTransformationDBCommunicator dbCommunicator)
    {
        this.dbCommunicator = dbCommunicator;
    }

    public async Task UpdateAsync(
        Hub hub,
        string farmIdStr,
        int playerId,
        TransformationDM transform)
    {
        // 🔊 Real-time broadcast
        await hub.Clients.OthersInGroup(farmIdStr)
            .SendAsync("PlayerTransformationUpdated", playerId, transform);

        if (!int.TryParse(farmIdStr, out int farmId))
            return;

        // ⏱ Throttle DB writes
        var now = DateTime.UtcNow;
        if (LastSave.TryGetValue(playerId, out var last) &&
            now - last < SaveInterval)
            return;

        LastSave[playerId] = now;

        // 💾 Persist via DB communicator
        await dbCommunicator
            .UpdateVisitorTransformationAsync(
                farmId,
                playerId,
                transform);
    }
}