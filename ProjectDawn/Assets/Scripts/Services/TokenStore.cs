using UnityEngine;

public static class TokenStore
{
    private const string AccessKey = "access";
    private const string RefreshKey = "refresh";

    public static void Load()
    {
        // PlayerPrefs loads automatically
        // Method kept for API symmetry
    }

    public static AuthTokens Get()
    {
        if (!PlayerPrefs.HasKey(AccessKey))
            return null;

        return new AuthTokens
        {
            AccessToken = PlayerPrefs.GetString(AccessKey),
            RefreshToken = PlayerPrefs.GetString(RefreshKey, null)
        };
    }

    public static void Set(AuthTokens tokens)
    {
        if (tokens == null)
            return;

        PlayerPrefs.SetString(AccessKey, tokens.AccessToken);
        PlayerPrefs.SetString(RefreshKey, tokens.RefreshToken);
        PlayerPrefs.Save(); // ✅ REQUIRED
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(AccessKey);
        PlayerPrefs.DeleteKey(RefreshKey);
        PlayerPrefs.Save(); // ✅ REQUIRED
    }
}