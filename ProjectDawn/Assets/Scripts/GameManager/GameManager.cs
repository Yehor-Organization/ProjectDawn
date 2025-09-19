using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;
[RequireComponent(typeof(ObjectManager))]
[RequireComponent(typeof(ProjectDawnApi))]
[RequireComponent(typeof(PlayerManager))]
public class GameManager : MonoBehaviour
{
    [Header("Game State")]

    [SerializeField] 
    private int playerId = 1;
    [SerializeField]
    private TMP_InputField playerIdInput; 

    [Header("Component References")]

    [SerializeField]
    private GameObject Menu;
    [SerializeField]
    private GameObject inGameUI;
    [SerializeField]
    private Button SettingsButton;

    private ObjectManager objectManager;
    private ProjectDawnApi projectDawnApi;
    private PlayerManager playerManager;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        objectManager = GetComponent<ObjectManager>();
        projectDawnApi = GetComponent<ProjectDawnApi>();
        playerManager = GetComponent<PlayerManager>();

        if (playerManager == null)
            Debug.LogError("[GameManager] PlayerManager component is missing!");
        if (objectManager == null)
            Debug.LogError("[GameManager] ObjectManager component is missing!");
        if (projectDawnApi == null)
            Debug.LogError("[GameManager] ProjectDawnApi component is missing!");
    }

    void Start()
    {
        playerIdInput.text = playerId.ToString();
        if (SettingsButton != null)
            SettingsButton.onClick.AddListener(ToggleMenu);

        if (playerIdInput != null)
            playerIdInput.onValueChanged.AddListener(OnPlayerIdChanged);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleMenu();
        }
    }
    private void OnPlayerIdChanged(string newValue)
    {
        if (int.TryParse(newValue, out int parsedId))
        {
            playerId = parsedId;
            Debug.Log($"[GameManager] Player ID updated: {playerId}");
        }
        else
        {
            Debug.LogWarning("[GameManager] Invalid Player ID input (not an integer).");
        }
    }
    private void ToggleMenu()
    {
        if (Menu == null) return;

        bool isActive = Menu.activeSelf;
        Menu.SetActive(!isActive);

        if (inGameUI != null)
            inGameUI.SetActive(isActive);
    }
    public async Task<bool> JoinFarm(string farmId)
    {
        // ✅ First leave current farm cleanly before joining another
        await LeaveFarm();

        // Optional: add a tiny delay to let SignalR fully close sockets
        await Task.Delay(300); // 0.3s buffer, tweak if needed

        if (projectDawnApi != null)
        {
            bool success = await projectDawnApi.ConnectAndJoin(farmId, playerId);
            if (success)
            {
                Debug.Log("[GameManager] Successfully joined farm.");
                return true;
            }
            else
            {
                Debug.LogError("[GameManager] Failed to join farm (server rejected or connection issue).");
                await ResetToMenu();
                return false;
            }
        }
        else
        {
            Debug.LogError("projectDawnApi (ProjectDawnApi) is not assigned!");
            await ResetToMenu();
            return false;
        }
    }

    public async Task ResetToMenu()
    {
        Debug.Log("[GameManager] Resetting game state to menu...");

        await LeaveFarm();

        var farmUI = FindObjectOfType<FarmListUI>();
        if (farmUI != null)
            farmUI.gameObject.SetActive(true);

        if (inGameUI != null)
            inGameUI.SetActive(false);

    }
    public async Task LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving current farm...");

        if (projectDawnApi != null)
            await projectDawnApi.DisconnectAsync();

        ClearFarm(); // ✅ now clear after disconnect
    }


    public void ForceLeaveFarmImmediate()
    {
        Debug.Log("[GameManager] Force leaving farm immediately...");

        playerManager.ClearAllPlayers();
        
        ClearFarm();

        if (Menu != null) Menu.SetActive(true);
        if (inGameUI != null) inGameUI.SetActive(false);

    }
    private void ClearFarm()
    {
        Debug.Log("[GameManager] Clearing farm state...");
        ObjectManager.Instance.ClearAll();
        playerManager.ClearAllPlayers();
    }



}

