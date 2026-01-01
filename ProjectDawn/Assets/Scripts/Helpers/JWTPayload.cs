using System;

public class JWTPayload
{
    public long exp { get; set; }

    public DateTime ExpUtc =>
        DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
}