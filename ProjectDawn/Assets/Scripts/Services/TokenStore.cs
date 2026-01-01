using UnityEngine;

public static class TokenStore
{
    public static void Clear()
    {
        PlayerPrefs.DeleteKey("access");
        PlayerPrefs.DeleteKey("refresh");
    }

    public static AuthTokens Get()
    {
        if (!PlayerPrefs.HasKey("access")) return null;

        return new AuthTokens
        {
            AccessToken = PlayerPrefs.GetString("access"),
            RefreshToken = PlayerPrefs.GetString("refresh")
        };
    }

    public static void Load()
    { }

    public static void Set(AuthTokens tokens)
    {
        PlayerPrefs.SetString("access", tokens.AccessToken);
        PlayerPrefs.SetString("refresh", tokens.RefreshToken);
    }
}