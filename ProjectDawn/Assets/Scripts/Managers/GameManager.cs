using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;

public class GameManager : MonoBehaviour
{
    private FarmAPICommunicator farmApi;
    private FarmHubCommunicator farmHub;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject Menu;

    // Cached references (lazy)
    private ObjectManager objectManager;

    private PlayerManager playerManager;
    [SerializeField] private Button SettingsButton;

    private FarmAPICommunicator FarmApi
    {
        get
        {
            if (farmApi == null)
                farmApi = Core.Instance?.ApiCommunicators?.FarmApi;

            if (farmApi == null)
                throw new InvalidOperationException("[GameManager] FarmApiCommunicator not available");

            return farmApi;
        }
    }

    private FarmHubCommunicator FarmHub
    {
        get
        {
            if (farmHub == null)
                farmHub = Core.Instance?.ApiCommunicators?.FarmHub;

            if (farmHub == null)
                throw new InvalidOperationException("[GameManager] FarmHubCommunicator not available");

            return farmHub;
        }
    }

    // -----------------------
    // Lazy dependencies
    // -----------------------
    private ObjectManager ObjectManager
    {
        get
        {
            if (objectManager == null)
                objectManager = Core.Instance?.Managers?.ObjectManager;

            if (objectManager == null)
                throw new InvalidOperationException("[GameManager] ObjectManager not available");

            return objectManager;
        }
    }

    private PlayerManager PlayerManager
    {
        get
        {
            if (playerManager == null)
                playerManager = Core.Instance?.Managers?.PlayerManager;

            if (playerManager == null)
                throw new InvalidOperationException("[GameManager] PlayerManager not available");

            return playerManager;
        }
    }

    // -----------------------
    // GAME FLOW
    // -----------------------
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

        var state = await FarmApi.GetFarmState(farmId);
        if (state == null)
        {
            Debug.LogError("[GameManager] Failed to load farm state");
            return false;
        }

        ClearFarm();
        BuildFarmFromState(state);

        bool connected = await FarmHub.Connect(farmId);
        if (!connected)
        {
            Debug.LogError("[GameManager] Failed to connect to farm hub");
            await ResetToMenu();
            return false;
        }

        Debug.Log("[GameManager] Successfully joined farm");
        return true;
    }

    public async Task LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving farm...");

        if (FarmHub.IsConnectedPublic)
            await FarmHub.Disconnect();

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

    // -----------------------
    // INTERNAL
    // -----------------------
    private void BuildFarmFromState(FarmStateDM state)
    {
        // TODO
    }

    private void ClearFarm()
    {
        Debug.Log("[GameManager] Clearing farm state");

        ObjectManager.ClearAll();
        PlayerManager.ClearAllPlayers();
    }

    private void Start()
    {
        if (SettingsButton != null)
            SettingsButton.onClick.AddListener(ToggleMenu);
    }

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