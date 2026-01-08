using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;

public class PlayerTransformationService
{
    // 🔑 NEW: Cache player transforms per farm (in-memory)
    private static readonly Dictionary<int, Dictionary<int, TransformationDM>>
        FarmPlayerTransforms = new();

    private static readonly Dictionary<int, DateTime> LastSave = new();
    private static readonly TimeSpan SaveInterval = TimeSpan.FromSeconds(60);
    private static readonly object TransformLock = new();

    private readonly PlayerTransformationDBCommunicator dbCommunicator;

    public PlayerTransformationService(
        PlayerTransformationDBCommunicator dbCommunicator)
    {
        this.dbCommunicator = dbCommunicator;
    }

    // 🔑 NEW: Get all cached transforms for a farm
    public Dictionary<int, TransformationDM> GetAllTransformsForFarm(int farmId, int? excludePlayerId = null)
    {
        lock (TransformLock)
        {
            if (!FarmPlayerTransforms.TryGetValue(farmId, out var players))
            {
                return new Dictionary<int, TransformationDM>();
            }

            var result = new Dictionary<int, TransformationDM>();
            foreach (var kvp in players)
            {
                // Skip the excluded player (usually the one joining)
                if (excludePlayerId.HasValue && kvp.Key == excludePlayerId.Value)
                    continue;

                result[kvp.Key] = kvp.Value;
            }

            return result;
        }
    }

    // 🔑 NEW: Clean up cache when player leaves
    public void RemovePlayerTransform(int farmId, int playerId)
    {
        lock (TransformLock)
        {
            if (FarmPlayerTransforms.TryGetValue(farmId, out var players))
            {
                players.Remove(playerId);

                // Clean up empty farms to prevent memory leaks
                if (players.Count == 0)
                {
                    FarmPlayerTransforms.Remove(farmId);
                }
            }
        }

        // Also clean up the save throttle
        LastSave.Remove(playerId);
    }

    public async Task UpdateAsync(
                Hub hub,
        string farmIdStr,
        int playerId,
        TransformationDM transform)
    {
        // ✅ AUTHORITATIVE SERVER TIMESTAMP (seconds)
        transform.serverTime =
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000f;

        if (!int.TryParse(farmIdStr, out int farmId))
            return;

        // 🔑 NEW: Cache the transform in memory
        lock (TransformLock)
        {
            if (!FarmPlayerTransforms.ContainsKey(farmId))
            {
                FarmPlayerTransforms[farmId] = new Dictionary<int, TransformationDM>();
            }
            FarmPlayerTransforms[farmId][playerId] = transform;
        }

        // 🔊 Real-time broadcast (others only — good)
        await hub.Clients.OthersInGroup(farmIdStr)
            .SendAsync(
                "PlayerTransformationUpdated",
                playerId,
                transform
            );

        // ⏱ Throttle DB writes
        var now = DateTime.UtcNow;
        if (LastSave.TryGetValue(playerId, out var last) &&
            now - last < SaveInterval)
            return;

        LastSave[playerId] = now;

        // 💾 Persist (same timestamp is fine)
        await dbCommunicator
            .UpdateVisitorTransformationAsync(
                farmId,
                playerId,
                transform
            );
    }
}