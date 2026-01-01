using System;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public static class JWTUtils
{
    public static JWTPayload Decode(string jwt)
    {
        if (string.IsNullOrEmpty(jwt))
            throw new ArgumentException("JWT is null or empty");

        var parts = jwt.Split('.');
        if (parts.Length != 3)
            throw new ArgumentException("Invalid JWT format");

        var payloadJson = Base64UrlDecode(parts[1]);
        var payload = JsonConvert.DeserializeObject<JWTPayload>(payloadJson);

        return payload;
    }

    private static string Base64UrlDecode(string input)
    {
        string padded = input
            .Replace('-', '+')
            .Replace('_', '/');

        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        var bytes = Convert.FromBase64String(padded);
        return Encoding.UTF8.GetString(bytes);
    }
}