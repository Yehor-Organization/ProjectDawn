using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    private FarmAPICommunicator farmApi;

    private FarmHubCommunicator farmHub;

    [SerializeField] private GameObject inGameUI;

    [Header("UI")]
    [SerializeField] private GameObject Menu;

    // 🔗 Dependencies
    private ObjectManager objectManager;

    private PlayerManager playerManager;
    [SerializeField] private Button SettingsButton;

    public void ForceLeaveFarmImmediate()
    {
        Debug.Log("[GameManager] Force leaving farm immediately");

        ClearFarm();

        if (Menu != null) Menu.SetActive(true);
        if (inGameUI != null) inGameUI.SetActive(false);
    }

    public async Task<bool> JoinFarm(string farmId)
    {
        await LeaveFarm();
        await Task.Delay(200);

        // (Optional) Load farm state first
        var state = await farmApi.GetFarmState(farmId);
        if (state == null)
        {
            Debug.LogError("[GameManager] Failed to load farm state");
            return false;
        }

        ClearFarm();
        BuildFarmFromState(state);

        bool connected = await farmHub.Connect(farmId);
        if (!connected)
        {
            Debug.LogError("[GameManager] Failed to connect to farm hub");
            await ResetToMenu();
            return false;
        }

        Debug.Log("[GameManager] Successfully joined farm");
        return true;
    }

    // -----------------------
    // GAME FLOW
    // -----------------------
    public async Task LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving farm...");

        if (farmHub != null)
            await farmHub.Disconnect();

        ClearFarm();
    }

    public async Task ResetToMenu()
    {
        await LeaveFarm();

        var farmUI = FindObjectOfType<FarmListUI>();
        if (farmUI != null)
            farmUI.gameObject.SetActive(true);

        if (inGameUI != null)
            inGameUI.SetActive(false);
    }

    private void Awake()
    {
        var managers = Core.Instance.Managers;
        var api = Core.Instance.ApiCommunicators;

        objectManager = managers.ObjectManager;
        playerManager = managers.PlayerManager;

        farmApi = api.FarmApi;
        farmHub = api.FarmHub;

        if (objectManager == null)
            Debug.LogError("[GameManager] ObjectManager missing");

        if (playerManager == null)
            Debug.LogError("[GameManager] PlayerManager missing");

        if (farmApi == null)
            Debug.LogError("[GameManager] FarmApiCommunicator missing");

        if (farmHub == null)
            Debug.LogError("[GameManager] FarmHubCommunicator missing");
    }

    private void BuildFarmFromState(FarmStateDM state)
    {
        // TODO: spawn objects / terrain / etc
        // objectManager.PlaceObject(...)
    }

    private void ClearFarm()
    {
        Debug.Log("[GameManager] Clearing farm state");

        objectManager?.ClearAll();
        playerManager?.ClearAllPlayers();
    }

    private void Start()
    {
        if (SettingsButton != null)
            SettingsButton.onClick.AddListener(ToggleMenu);
    }

    // -----------------------
    // INTERNAL
    // -----------------------
    private void ToggleMenu()
    {
        if (Menu == null) return;

        bool isActive = Menu.activeSelf;
        Menu.SetActive(!isActive);

        if (inGameUI != null)
            inGameUI.SetActive(isActive);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ToggleMenu();
    }
}