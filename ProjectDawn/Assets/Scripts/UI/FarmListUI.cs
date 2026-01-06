using UnityEngine;
using System;
using System.Threading.Tasks;

public class FarmListUI : MonoBehaviour
{
    [SerializeField] private RectTransform farmListContainer;
    [SerializeField] private GameObject farmListItemPrefab;
    private GameManager gameManager;

    private FarmAPICommunicator FarmApi =>
        Core.Instance?.ApiCommunicators?.FarmApi;

    private FarmHubCommunicator FarmHub =>
        Core.Instance?.ApiCommunicators?.FarmHub;

    private GameManager GameManager =>
        gameManager ??= Core.Instance?.Managers?.GameManager
        ?? throw new InvalidOperationException(
            "[FarmListUI] GameManager missing");

    private async void HandleFarmListUpdated()
    {
        await PopulateFarmListAsync();
    }

    private void OnDisable()
    {
        if (FarmHub != null)
            FarmHub.OnFarmListUpdated -= HandleFarmListUpdated;
    }

    private async void OnEnable()
    {
        while (FarmApi == null || FarmHub == null)
            await Task.Yield();

        await PopulateFarmListAsync();
        FarmHub.OnFarmListUpdated += HandleFarmListUpdated;
    }

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
            var item = Instantiate(
                farmListItemPrefab,
                farmListContainer
            );

            var ui = item.GetComponent<FarmListItemUI>();
            ui?.Setup(farm, GameManager, this);
        }
    }
}