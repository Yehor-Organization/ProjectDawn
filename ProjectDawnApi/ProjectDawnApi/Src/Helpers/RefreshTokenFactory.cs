using System.Security.Cryptography;

namespace ProjectDawnApi;

public static class RefreshTokenFactory
{
    public static string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
