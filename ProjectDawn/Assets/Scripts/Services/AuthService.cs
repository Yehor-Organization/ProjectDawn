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

    private readonly SemaphoreSlim initLock = new(1, 1);
    [SerializeField] private AuthAPICommunicator authApi;

    private CancellationTokenSource refreshCts;
    private Task<AuthTokens> refreshTask;
    private AuthTokens tokens;

    private bool isInitialized;

    // =========================
    // UNITY MAIN THREAD CONTEXT
    // =========================

    private SynchronizationContext unityContext;

    // =========================
    // LAZY UI MANAGER ACCESS
    // =========================

    public bool IsLoggedIn => tokens != null && !IsExpiredSafe(tokens.AccessToken);

    private UIManager UIManager =>
        Core.Instance.Managers.UIManager;

    // =========================
    // UNITY
    // =========================

    public async Task Login(string username, string password)
    {
        await EnsureInitializedAsync();
        ApplyNewTokens(await authApi.Login(username, password));
    }

    // =========================
    // PUBLIC AUTH API
    // =========================
    public async Task Register(string username, string password)
    {
        await EnsureInitializedAsync();
        ApplyNewTokens(await authApi.Register(username, password));
    }

    public void Logout()
    {
        EnsureInitializedSync();
        Invalidate(AuthInvalidReason.TokensMissing);
    }

    /// <summary>
    /// Returns a valid access token or null.
    /// UI is handled separately.
    /// </summary>
    public async Task<string> GetValidAccessToken()
    {
        await EnsureInitializedAsync();

        if (tokens == null)
            return null;

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
        unityContext = SynchronizationContext.Current;
    }

    // =========================
    // INITIALIZATION
    // =========================

    private async Task EnsureInitializedAsync()
    {
        if (isInitialized)
            return;

        await initLock.WaitAsync();
        try
        {
            if (isInitialized)
                return;

            TokenStore.Load();
            tokens = TokenStore.Get();

            if (tokens != null && !IsExpiredSafe(tokens.AccessToken))
            {
                Debug.Log("[AuthService] Valid token loaded");
                StartAutoRefresh();
            }
            else
            {
                Debug.Log("[AuthService] No valid token");
                tokens = null;
            }

            isInitialized = true;
        }
        finally
        {
            initLock.Release();
        }

        SyncUI();
    }

    private void EnsureInitializedSync()
    {
        if (isInitialized)
            return;

        TokenStore.Load();
        tokens = TokenStore.Get();
        isInitialized = true;

        SyncUI();
    }

    // =========================
    // INTERNAL
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
        SyncUI();
    }

    private void Invalidate(AuthInvalidReason reason)
    {
        StopAutoRefresh();
        TokenStore.Clear();

        tokens = null;
        refreshTask = null;

        Debug.Log($"[AuthService] Invalidated: {reason}");

        SyncUI();
    }

    // =========================
    // UI SYNC (SINGLE SOURCE)
    // =========================

    private void SyncUI()
    {
        Debug.Log("[AuthService] SyncUI is called.");

        if (IsLoggedIn)
        {
            UIManager.ShowMenu();

            Debug.Log("[AuthService] user logged in.");
        }
        else
        {
            UIManager.ShowLogin();
            Debug.Log("[AuthService] user is not logged in.");
        }
    }

    // =========================
    // JWT
    // =========================

    private bool IsExpired(string jwt)
    {
        var payload = JWTUtils.Decode(jwt);
        return payload.ExpUtc <= DateTime.UtcNow.AddSeconds(30);
    }

    private bool IsExpiredSafe(string jwt)
    {
        try { return IsExpired(jwt); }
        catch { return true; }
    }

    // =========================
    // REFRESH
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
            SyncUI();
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