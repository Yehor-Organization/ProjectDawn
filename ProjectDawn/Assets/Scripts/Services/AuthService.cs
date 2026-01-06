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
    private const string LogTag = "[AuthService]";

    private readonly SemaphoreSlim initLock = new(1, 1);

    [SerializeField] private AuthAPICommunicator authApi;

    private CancellationTokenSource refreshCts;
    private Task<AuthTokens> refreshTask;
    private AuthTokens tokens;

    private bool isInitialized;
    private Task initializationTask;

    // =========================
    // UNITY MAIN THREAD CONTEXT
    // =========================

    private SynchronizationContext unityContext;

    // =========================
    // CACHED UI MANAGER
    // =========================

    private UIManager uiManager;

    // =========================
    // AUTH STATE
    // =========================

    public bool IsLoggedIn =>
        tokens != null && !IsExpiredSafe(tokens.AccessToken);

    // =========================
    // LOGGING
    // =========================

    private void Log(string message)
        => Debug.Log($"{LogTag} {message}");

    private void LogWarn(string message)
        => Debug.LogWarning($"{LogTag} {message}");

    private void LogError(string message)
        => Debug.LogError($"{LogTag} {message}");

    // =========================
    // UNITY
    // =========================

    private void Awake()
    {
        unityContext = SynchronizationContext.Current;
        Log("Awake()");
    }

    private async void Start()
    {
        Log("Start() — waiting for UIManager");

        await EnsureUIManagerAsync();

        Log("UIManager ready — initializing auth");

        initializationTask = EnsureInitializedAsync();
        await initializationTask;
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
            PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    // =========================
    // PUBLIC AUTH API
    // =========================

    public async Task Login(string username, string password)
    {
        Log($"Login attempt for '{username}'");

        await EnsureInitializedAsync();
        ApplyNewTokens(await authApi.Login(username, password));
    }

    public async Task Register(string username, string password)
    {
        Log($"Register attempt for '{username}'");

        await EnsureInitializedAsync();
        ApplyNewTokens(await authApi.Register(username, password));
    }

    public void Logout()
    {
        EnsureInitializedSync();
        Invalidate(AuthInvalidReason.TokensMissing);
    }

    /// <summary>
    /// HARD AUTH GATE.
    /// Any system that requires auth MUST await this.
    /// </summary>
    public async Task WaitUntilLoggedInAsync()
    {
        while (!IsLoggedIn)
            await Task.Yield();
    }

    /// <summary>
    /// Returns a valid access token or null.
    /// </summary>
    public async Task<string> GetValidAccessToken()
    {
        await EnsureInitializedAsync();

        if (tokens == null)
        {
            Log("GetValidAccessToken → no tokens");
            return null;
        }

        try
        {
            if (!IsExpired(tokens.AccessToken))
            {
                Log("Access token valid");
                return tokens.AccessToken;
            }
        }
        catch
        {
            LogError("Access token corrupted");
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return null;
        }

        if (string.IsNullOrEmpty(tokens.RefreshToken))
        {
            LogWarn("Token expired and refresh token missing");
            Invalidate(AuthInvalidReason.TokenExpired);
            return null;
        }

        if (refreshTask == null)
        {
            Log("Starting refresh task");
            refreshTask = RefreshInternal();
        }

        try
        {
            tokens = await refreshTask;
            Log("Token refresh successful");

            StartAutoRefresh();
            return tokens.AccessToken;
        }
        catch
        {
            LogError("Token refresh failed");
            Invalidate(AuthInvalidReason.RefreshFailed);
            return null;
        }
        finally
        {
            refreshTask = null;
        }
    }

    // =========================
    // ENSURE UI MANAGER
    // =========================

    private async Task EnsureUIManagerAsync()
    {
        if (uiManager != null)
            return;

        uiManager = await CoreWaitHelpers.WaitForManagerAsync(m => m.UIManager);
    }

    // =========================
    // INITIALIZATION
    // =========================

    private async Task EnsureInitializedAsync()
    {
        if (isInitialized)
            return;

        Log("EnsureInitializedAsync() entered");

        await initLock.WaitAsync();
        try
        {
            if (isInitialized)
                return;

            TokenStore.Load();
            tokens = TokenStore.Get();

            if (tokens != null && !IsExpiredSafe(tokens.AccessToken))
            {
                Log("Valid token loaded from storage");
                StartAutoRefresh();
            }
            else
            {
                Log("No valid stored token");
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
            LogError("ApplyNewTokens received null");
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return;
        }

        tokens = newTokens;
        TokenStore.Set(newTokens);

        Log("Authentication successful");

        StartAutoRefresh();
        SyncUI();
    }

    private void Invalidate(AuthInvalidReason reason)
    {
        StopAutoRefresh();
        TokenStore.Clear();

        tokens = null;
        refreshTask = null;

        LogWarn($"Invalidated → {reason}");

        SyncUI();
    }

    // =========================
    // UI SYNC
    // =========================

    private async void SyncUI()
    {
        await EnsureUIManagerAsync();

        if (IsLoggedIn)
        {
            Log("UI → ShowMenu()");
            uiManager.ShowMenu();
        }
        else
        {
            Log("UI → ShowLogin()");
            uiManager.ShowLogin();
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
        Log("Refreshing token");
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
            Log("Immediate refresh triggered");

            refreshTask = RefreshInternal();
            tokens = await refreshTask;

            StartAutoRefresh();
            SyncUI();
        }
        catch
        {
            LogError("Immediate refresh failed");
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
        {
            Log("Auto-refresh skipped — no refresh token");
            return;
        }

        JWTPayload payload;
        try
        {
            payload = JWTUtils.Decode(tokens.AccessToken);
        }
        catch
        {
            LogError("JWT decode failed during auto-refresh setup");
            Invalidate(AuthInvalidReason.TokenCorrupted);
            return;
        }

        var refreshAt = payload.ExpUtc.AddSeconds(-RefreshEarlySeconds);
        var delay = refreshAt - DateTime.UtcNow;

        Log($"Auto-refresh scheduled in {delay.TotalSeconds:F1}s");

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
