using UnityEngine;
using System;
using System.Collections;
using System.Threading.Tasks;

public class FarmListUI : MonoBehaviour
{
    private AuthService authService;

    private bool bootstrapped;

    [Header("UI")]
    [SerializeField] private RectTransform farmListContainer;

    [SerializeField] private GameObject farmListItemPrefab;

    private GameManager gameManager;

    // =====================
    // DEPENDENCIES
    // =====================
    private bool populateInProgress;

    private int populateVersion;

    private AuthService Auth =>
        authService ??= Core.Instance?.Services?.AuthService
        ?? throw new InvalidOperationException(
            "[FarmListUI] AuthService not available");

    private FarmAPICommunicator FarmApi =>
            Core.Instance?.ApiCommunicators?.FarmApi;

    private FarmListHubCommunicator FarmListHub =>
        Core.Instance?.ApiCommunicators?.FarmListHub;

    private GameManager GameManager =>
        gameManager ??= Core.Instance?.Managers?.GameManager
        ?? throw new InvalidOperationException(
            "[FarmListUI] GameManager missing");

    // =====================
    // UNITY
    // =====================

    private async Task BootstrapAsync()
    {
        while (FarmApi == null || FarmListHub == null)
            await Task.Yield();

        // 2️⃣ Wait for authentication
        await Auth.WaitUntilLoggedInAsync();

        // 3️⃣ Initial load
        await PopulateFarmListAsync();

        // 4️⃣ Safe subscription (idempotent)
        FarmListHub.OnFarmListUpdated -= HandleFarmListUpdated;
        FarmListHub.OnFarmListUpdated += HandleFarmListUpdated;
    }

    private IEnumerator BootstrapNextFrame()
    {
        // Let UI render and Unity stabilize
        yield return null;

        _ = BootstrapAsync();
    }

    private void HandleFarmListUpdated()
    {
        PopulateFarmListAsync();
    }

    private void OnDisable()
    {
        if (FarmListHub != null)
            FarmListHub.OnFarmListUpdated -= HandleFarmListUpdated;

        bootstrapped = false;
    }

    private void OnEnable()
    {
        // Prevent double bootstrap when menu toggles
        if (bootstrapped)
            return;

        bootstrapped = true;

        // Delay heavy work until AFTER the frame finishes
        StartCoroutine(BootstrapNextFrame());
    }

    // =====================
    // BOOTSTRAP
    // =====================
    // =====================
    // EVENTS
    // =====================
    // =====================
    // DATA
    // =====================

    private async Task PopulateFarmListAsync()
    {
        if (farmListContainer == null || FarmApi == null)
            return;

        // 🔒 Prevent overlapping populates
        if (populateInProgress)
            return;

        populateInProgress = true;
        int myVersion = ++populateVersion;

        try
        {
            var farms = await FarmApi.GetAllFarms();

            // 🛑 A newer populate started while we awaited
            if (myVersion != populateVersion)
                return;

            if (farms == null)
                return;

            // Clear UI
            for (int i = farmListContainer.childCount - 1; i >= 0; i--)
                Destroy(farmListContainer.GetChild(i).gameObject);

            // Build UI
            foreach (var farm in farms)
            {
                var item = Instantiate(farmListItemPrefab, farmListContainer);
                var ui = item.GetComponent<FarmListItemUI>();
                ui?.Setup(farm, GameManager, this);
            }
        }
        finally
        {
            populateInProgress = false;
        }
    }
}