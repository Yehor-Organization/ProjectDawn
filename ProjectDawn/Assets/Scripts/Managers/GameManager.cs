using UnityEngine;
using System;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    private FarmAPICommunicator farmApi;
    private FarmHubCommunicator farmHub;

    private bool inFarm;
    private bool joiningFarm;
    private ObjectManager objectManager;
    private PlayerManager playerManager;
    private UIManager uiManager;

    // =======================
    // Lazy dependencies
    // =======================

    private FarmAPICommunicator FarmApi
    {
        get
        {
            if (farmApi == null)
                farmApi = Core.Instance?.ApiCommunicators?.FarmApi;

            if (farmApi == null)
                throw new InvalidOperationException("[GameManager] FarmApi not available");

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
                throw new InvalidOperationException("[GameManager] FarmHub not available");

            return farmHub;
        }
    }

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

    private UIManager UIManager
    {
        get
        {
            if (uiManager != null)
                return uiManager;

            if (Core.Instance?.Managers == null)
                return null;

            uiManager = Core.Instance.Managers.UIManager;
            return uiManager;
        }
    }

    // =======================
    // GAME FLOW (PUBLIC API)
    // =======================

    public async Task<bool> JoinFarm(string farmId)
    {
        if (joiningFarm)
        {
            Debug.LogWarning("[GameManager] JoinFarm ignored (already joining)");
            return false;
        }

        joiningFarm = true;

        try
        {
            Debug.Log($"[GameManager] Joining farm {farmId}");

            await LeaveFarm();

            var state = await FarmApi.GetFarmState(farmId);
            if (state == null)
                return false;

            ClearFarm();
            BuildFarmFromState(state);

            if (!await FarmHub.Connect(farmId))
                return false;

            inFarm = true;
            UIManager?.ShowGameUI();

            return true;
        }
        finally
        {
            joiningFarm = false;
        }
    }

    public async Task LeaveFarm()
    {
        if (!inFarm && !joiningFarm)
            return;

        Debug.Log("[GameManager] Leaving farm");

        if (FarmHub != null && FarmHub.IsConnectedPublic)
            await FarmHub.Disconnect();

        ClearFarm();
        inFarm = false;
    }

    public async Task ResetToMenu()
    {
        Debug.Log("[GameManager] Resetting to menu");

        await LeaveFarm();

        if (UIManager != null)
            UIManager.ShowMenu();
        else
            Debug.LogWarning("[GameManager] UIManager not ready yet (ShowMenu skipped)");
    }

    // =======================
    // INTERNAL
    // =======================

    private void BuildFarmFromState(FarmStateDM state)
    {
        // TODO: spawn farm objects, terrain, crops, etc.
        Debug.Log("[GameManager] Building farm from state");
    }

    private void ClearFarm()
    {
        Debug.Log("[GameManager] Clearing farm state");

        if (objectManager != null)
            objectManager.ClearAll();
    }
}