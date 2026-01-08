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

        var farms = await FarmApi.GetAllFarms();
        if (farms == null)
            return;

        Debug.Log($"FARMS COUNT = {farms.Count}");

        Debug.Log("CLEAR UI START");
        for (int i = farmListContainer.childCount - 1; i >= 0; i--)
            Destroy(farmListContainer.GetChild(i).gameObject);
        Debug.Log("CLEAR UI END");

        Debug.Log("INSTANTIATE START");
        foreach (var farm in farms)
        {
            var item = Instantiate(farmListItemPrefab, farmListContainer);
            var ui = item.GetComponent<FarmListItemUI>();
            ui?.Setup(farm, GameManager, this);
        }
        Debug.Log("INSTANTIATE END");
    }
}