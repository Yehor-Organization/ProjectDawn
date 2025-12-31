using System.Collections.Concurrent;

namespace ProjectDawnApi.Src.Services.Player;

public static class PlayerConnectionRegistry
{
    public static readonly ConcurrentDictionary<int, string> ActiveConnections = new();
}
