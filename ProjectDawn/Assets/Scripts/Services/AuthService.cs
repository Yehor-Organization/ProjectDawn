using System;
using System.Threading.Tasks;
using UnityEngine;

public class AuthService : MonoBehaviour
{
    [SerializeField] private AuthAPICommunicator authApi;

    // 🔒 Prevent double refresh
    private Task<AuthTokens> refreshTask;

    private AuthTokens tokens;

    public async Task<string> GetValidAccessToken()
    {
        if (tokens == null)
            return null;

        // ✅ Token still valid
        if (!IsExpired(tokens.AccessToken))
            return tokens.AccessToken;

        // ❌ Can't refresh
        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            Logout();
            return null;
        }

        // 🔁 Ensure only ONE refresh happens
        if (refreshTask == null)
            refreshTask = RefreshInternal();

        try
        {
            tokens = await refreshTask;
            return tokens.AccessToken;
        }
        catch
        {
            Logout();
            return null;
        }
        finally
        {
            refreshTask = null;
        }
    }

    public void Logout()
    {
        TokenStore.Clear();
        tokens = null;
        refreshTask = null;
    }

    private void Awake()
    {
        TokenStore.Load();
        tokens = TokenStore.Get();
    }

    private bool IsExpired(string jwt)
    {
        var payload = JWTUtils.Decode(jwt);

        // ⏱ Refresh a bit early to avoid race conditions
        return payload.ExpUtc <= DateTime.UtcNow.AddSeconds(30);
    }

    private async Task<AuthTokens> RefreshInternal()
    {
        var refreshed = await authApi.Refresh(tokens.RefreshToken);
        TokenStore.Set(refreshed);
        return refreshed;
    }
}