using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class FarmListUI : MonoBehaviour
{
    // 🔗 Dependencies
    private FarmAPICommunicator farmApi;

    [Header("UI")]
    [SerializeField] private RectTransform farmListContainer;

    [SerializeField] private GameObject farmListItemPrefab;
    private GameManager gameManager;
    [SerializeField] private GameObject inGameUI;

    [Header("Settings")]
    [SerializeField] private float refreshPeriod = 1f;

    private CancellationTokenSource refreshToken;

    public void FarmJoined()
    {
        inGameUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        var core = Core.Instance;

        farmApi = core.ApiCommunicators.FarmApi;
        gameManager = core.Managers.GameManager;

        if (farmApi == null)
            Debug.LogError("[FarmListUI] FarmApiCommunicator missing");

        if (gameManager == null)
            Debug.LogError("[FarmListUI] GameManager missing");
    }

    private void OnDisable()
    {
        refreshToken?.Cancel();
        refreshToken = null;
    }

    private void OnEnable()
    {
        inGameUI.SetActive(false);
        refreshToken = new CancellationTokenSource();
        _ = RefreshLoopAsync(refreshToken.Token);
    }

    // -----------------------
    // ASYNC LOOP (SAFE)
    // -----------------------

    // -----------------------
    // UI LOGIC
    // -----------------------
    private async Task PopulateFarmListAsync()
    {
        var farms = await farmApi.GetAllFarms();
        if (farms == null) return;

        // Clear existing items
        foreach (Transform child in farmListContainer)
            Destroy(child.gameObject);

        foreach (var farm in farms)
        {
            var item = Instantiate(farmListItemPrefab, farmListContainer);
            var farmItemUI = item.GetComponent<FarmListItemUI>();

            if (farmItemUI != null)
                farmItemUI.Setup(farm, gameManager, this);
        }
    }

    private async Task RefreshLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            await PopulateFarmListAsync();

            try
            {
                await Task.Delay((int)(refreshPeriod * 1000), token);
            }
            catch (TaskCanceledException) { }
        }
    }
}