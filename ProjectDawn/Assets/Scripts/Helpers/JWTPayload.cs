using System;
using Newtonsoft.Json;

public class JWTPayload
{
    // =========================
    // Player identity
    // =========================

    // Standard JWT subject = user/player id
    [JsonProperty("sub")]
    public int PlayerId { get; set; }

    // =========================
    // Expiration
    // =========================

    // Unix timestamp (seconds)
    [JsonProperty("exp")]
    public long Exp { get; set; }

    // =========================
    // Helpers
    // =========================

    [JsonIgnore]
    public DateTime ExpUtc =>
        DateTimeOffset.FromUnixTimeSeconds(Exp).UtcDateTime;

    [JsonIgnore]
    public bool IsExpired =>
        DateTime.UtcNow >= ExpUtc;
}