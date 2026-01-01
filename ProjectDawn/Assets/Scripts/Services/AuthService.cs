using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public enum AuthInvalidReason
{
    TokensMissing,
    TokenExpired,
    RefreshFailed,
    TokenCorrupted
}

public class AuthService : MonoBehaviour
{
    private const int RefreshEarlySeconds = 60;

    [SerializeField] private AuthAPICommunicator authApi;

    private CancellationTokenSource refreshCts;
    private Task<AuthTokens> refreshTask;
    private AuthTokens tokens;

    // =========================
    // EVENTS
    // =========================

    /// <summary>
    /// Fired when authentication becomes valid (login / refresh success)
    /// </summary>
    public event Action Authenticated;

    /// <summary>
    /// Fired when authentication becomes invalid
    /// </summary>
    public event Action<AuthInvalidReason> AuthInvalidated;

    // =========================
    // STATE
    // =========================

    public bool IsLoggedIn => tokens != null;

    public bool HasValidAccessToken
    {
        get
        {
            if (tokens == null)
                return false;

            try
            {
                return !IsExpired(tokens.AccessToken);
            }
            catch
            {
                return false;
            }
        }
    }

    // =========================
    // UNITY LIFECYCLE
    // =========================

    public async Task Login(string username, string password)
    {
        var newTokens = await authApi.Login(username, password);
        ApplyNewTokens(newTokens);
    }

    // =========================
    // PUBLIC AUTH API
    // =========================
    public async Task Register(string username, string password)
    {
        var newTokens = await authApi.Register(username, password);
        ApplyNewTokens(newTokens);
    }

    public void Logout()
    {
        Invalidate(AuthInvalidReason.TokensMissing);
    }

    /// <summary>
    /// Returns a valid access token or null if auth is invalid
    /// </summary>
    public async Task<string> GetValidAccessToken()
    {
        if (tokens == null)
        {
            Invalidate(AuthInvalidReason.TokensMissing);
            return null;
        }

        try
        {
            if (!IsExpired(tokens.AccessToken))
                return tokens.AccessToken;
        }
        catch
        {
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return null;
        }

        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            Invalidate(AuthInvalidReason.TokenExpired);
            return null;
        }

        if (refreshTask == null)
            refreshTask = RefreshInternal();

        try
        {
            tokens = await refreshTask;
            StartAutoRefresh();
            return tokens.AccessToken;
        }
        catch
        {
            Invalidate(AuthInvalidReason.RefreshFailed);
            return null;
        }
        finally
        {
            refreshTask = null;
        }
    }

    private void Awake()
    {
        TokenStore.Load();
        tokens = TokenStore.Get();

        if (tokens != null)
        {
            Debug.Log("[AuthService] Tokens loaded from storage");
            StartAutoRefresh();
            Authenticated?.Invoke();
        }
        else
        {
            Invalidate(AuthInvalidReason.TokensMissing);
        }
    }

    private void OnDestroy()
    {
        StopAutoRefresh();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && tokens != null)
            StartAutoRefresh();
    }

    // =========================
    // INTERNAL TOKEN HANDLING
    // =========================

    private void ApplyNewTokens(AuthTokens newTokens)
    {
        if (newTokens == null)
        {
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return;
        }

        tokens = newTokens;
        TokenStore.Set(newTokens);

        Debug.Log("[AuthService] Authenticated");

        StartAutoRefresh();
        Authenticated?.Invoke();
    }

    private void Invalidate(AuthInvalidReason reason)
    {
        StopAutoRefresh();
        TokenStore.Clear();

        tokens = null;
        refreshTask = null;

        Debug.Log($"[AuthService] Invalidated: {reason}");
        AuthInvalidated?.Invoke(reason);
    }

    // =========================
    // TOKEN EXPIRY
    // =========================

    private bool IsExpired(string jwt)
    {
        var payload = JWTUtils.Decode(jwt);
        return payload.ExpUtc <= DateTime.UtcNow.AddSeconds(30);
    }

    // =========================
    // AUTO REFRESH
    // =========================

    private async Task<AuthTokens> RefreshInternal()
    {
        var refreshed = await authApi.Refresh(tokens.RefreshToken);
        TokenStore.Set(refreshed);
        return refreshed;
    }

    private async Task RefreshNow()
    {
        if (refreshTask != null)
            return;

        try
        {
            refreshTask = RefreshInternal();
            tokens = await refreshTask;
            StartAutoRefresh();
            Authenticated?.Invoke();
        }
        catch
        {
            Invalidate(AuthInvalidReason.RefreshFailed);
        }
        finally
        {
            refreshTask = null;
        }
    }

    private void StartAutoRefresh()
    {
        StopAutoRefresh();

        if (tokens == null || string.IsNullOrEmpty(tokens.RefreshToken))
            return;

        JWTPayload payload;

        try
        {
            payload = JWTUtils.Decode(tokens.AccessToken);
        }
        catch
        {
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return;
        }

        var refreshAt = payload.ExpUtc.AddSeconds(-RefreshEarlySeconds);
        var delay = refreshAt - DateTime.UtcNow;

        if (delay <= TimeSpan.Zero)
        {
            _ = RefreshNow();
            return;
        }

        refreshCts = new CancellationTokenSource();
        _ = AutoRefreshLoop(delay, refreshCts.Token);
    }

    private async Task AutoRefreshLoop(TimeSpan delay, CancellationToken token)
    {
        try
        {
            await Task.Delay(delay, token);
            await RefreshNow();
        }
        catch (TaskCanceledException) { }
    }

    private void StopAutoRefresh()
    {
        refreshCts?.Cancel();
        refreshCts = null;
    }
}