using System.Collections.Concurrent;

namespace ProjectDawnApi;

public static class PlayerConnectionRegistry
{
    public static readonly ConcurrentDictionary<int, string> ActiveConnections = new();
}