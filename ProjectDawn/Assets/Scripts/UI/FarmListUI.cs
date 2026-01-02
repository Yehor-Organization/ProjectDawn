using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public class FarmListUI : MonoBehaviour
{
    // =======================
    // Dependencies (LAZY)
    // =======================

    private FarmAPICommunicator farmApi;
    private GameManager gameManager;

    [Header("UI")]
    [SerializeField] private RectTransform farmListContainer;

    [SerializeField] private GameObject farmListItemPrefab;
    [SerializeField] private GameObject inGameUI;

    [Header("Settings")]
    [SerializeField] private float refreshPeriod = 1f;

    private CancellationTokenSource refreshToken;
    private CancellationTokenSource enableCts;

    private bool refreshStarted;

    // =======================
    // Lazy accessors
    // =======================

    private FarmAPICommunicator FarmApi
    {
        get
        {
            if (farmApi == null)
                farmApi = Core.Instance?.ApiCommunicators?.FarmApi;

            if (farmApi == null)
                throw new InvalidOperationException(
                    "[FarmListUI] FarmAPICommunicator not available.");

            return farmApi;
        }
    }

    private GameManager GameManager
    {
        get
        {
            if (gameManager == null)
                gameManager = Core.Instance?.Managers?.GameManager;

            if (gameManager == null)
                throw new InvalidOperationException(
                    "[FarmListUI] GameManager not available.");

            return gameManager;
        }
    }

    // =======================
    // Unity lifecycle
    // =======================

    public void FarmJoined()
    {
        inGameUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        if (farmListContainer == null)
            Debug.LogError("[FarmListUI] farmListContainer not assigned");

        if (farmListItemPrefab == null)
            Debug.LogError("[FarmListUI] farmListItemPrefab not assigned");

        if (inGameUI == null)
            Debug.LogError("[FarmListUI] inGameUI not assigned");
    }

    private void OnEnable()
    {
        inGameUI.SetActive(false);

        refreshStarted = false;

        enableCts = new CancellationTokenSource();
        _ = WaitForAuthAndStartAsync(enableCts.Token);
    }

    private void OnDisable()
    {
        refreshStarted = false;

        enableCts?.Cancel();
        enableCts = null;

        refreshToken?.Cancel();
        refreshToken = null;

        var auth = Core.Instance?.Services?.AuthService;
        if (auth != null)
            auth.Authenticated -= OnAuthenticated;
    }

    // =======================
    // Auth gate (FIXED)
    // =======================

    private async Task WaitForAuthAndStartAsync(CancellationToken token)
    {
        // Wait for Core
        while (Core.Instance == null)
        {
            if (token.IsCancellationRequested) return;
            await Task.Yield();
        }

        // Wait for AuthService
        while (Core.Instance.Services?.AuthService == null)
        {
            if (token.IsCancellationRequested) return;
            await Task.Yield();
        }

        var auth = Core.Instance.Services.AuthService;

        // 🔑 CRITICAL FIX
        if (auth.HasValidAccessToken)
        {
            Debug.Log("[FarmListUI] Already authenticated → starting refresh");
            StartRefresh();
        }
        else
        {
            Debug.Log("[FarmListUI] Waiting for authentication...");
            auth.Authenticated -= OnAuthenticated;
            auth.Authenticated += OnAuthenticated;
        }
    }

    private void OnAuthenticated()
    {
        var auth = Core.Instance?.Services?.AuthService;
        if (auth != null)
            auth.Authenticated -= OnAuthenticated;

        Debug.Log("[FarmListUI] Authenticated → starting refresh");
        StartRefresh();
    }

    // =======================
    // Refresh loop
    // =======================

    private void StartRefresh()
    {
        if (refreshStarted)
            return;

        refreshStarted = true;

        refreshToken?.Cancel();
        refreshToken = new CancellationTokenSource();
        _ = RefreshLoopAsync(refreshToken.Token);
    }

    private async Task RefreshLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                await PopulateFarmListAsync();
                await Task.Delay((int)(refreshPeriod * 1000), token);
            }
            catch (TaskCanceledException)
            {
                // expected
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FarmListUI] Refresh loop error: {ex}");
                await Task.Delay(1000);
            }
        }
    }

    // =======================
    // UI population
    // =======================

    private async Task PopulateFarmListAsync()
    {
        var farms = await FarmApi.GetAllFarms();
        if (farms == null)
            return;

        foreach (Transform child in farmListContainer)
            Destroy(child.gameObject);

        foreach (var farm in farms)
        {
            var item = Instantiate(farmListItemPrefab, farmListContainer);
            var farmItemUI = item.GetComponent<FarmListItemUI>();

            if (farmItemUI != null)
                farmItemUI.Setup(farm, GameManager, this);
        }
    }

    // =======================
    // Public
    // =======================
}