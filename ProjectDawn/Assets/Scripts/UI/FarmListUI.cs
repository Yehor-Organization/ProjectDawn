using UnityEngine;
using System.Threading.Tasks;
using System;

public class FarmListUI : MonoBehaviour
{
    // =======================
    // Dependencies (lazy)
    // =======================

    private GameManager gameManager;

    [Header("UI")]
    [SerializeField] private RectTransform farmListContainer;

    // =======================
    // UI
    // =======================
    [SerializeField] private GameObject farmListItemPrefab;

    private FarmAPICommunicator FarmApi =>
                Core.Instance?.ApiCommunicators?.FarmApi;

    private FarmHubCommunicator FarmHub =>
        Core.Instance?.ApiCommunicators?.FarmHub;

    private GameManager GameManager =>
        gameManager ??= Core.Instance?.Managers?.GameManager
        ?? throw new InvalidOperationException("[FarmListUI] GameManager missing");

    // =======================
    // Unity lifecycle
    // =======================

    private async void OnEnable()
    {
        // ⏳ Wait for Core + API + SignalR
        while (FarmApi == null || FarmHub == null)
            await Task.Yield();

        // 🌱 Initial load (REST)
        await PopulateFarmListAsync();

        // 🔔 Subscribe to SignalR updates
        FarmHub.OnFarmListUpdated += HandleFarmListUpdated;
        FarmHub.OnFarmJoined += HandleFarmJoined;
    }

    private void OnDisable()
    {
        if (FarmHub != null)
        {
            FarmHub.OnFarmListUpdated -= HandleFarmListUpdated;
            FarmHub.OnFarmJoined -= HandleFarmJoined;
        }
    }

    // =======================
    // SignalR handlers
    // =======================

    private async void HandleFarmListUpdated()
    {
        await PopulateFarmListAsync();
    }

    private void HandleFarmJoined(FarmInfoDTO farm)
    {
        if (farm == null)
            return;

        var item = Instantiate(farmListItemPrefab, farmListContainer);
        var farmItemUI = item.GetComponent<FarmListItemUI>();

        if (farmItemUI != null)
            farmItemUI.Setup(farm, GameManager, this);
    }

    // =======================
    // UI population
    // =======================

    private async Task PopulateFarmListAsync()
    {
        if (FarmApi == null || farmListContainer == null)
            return;

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
}