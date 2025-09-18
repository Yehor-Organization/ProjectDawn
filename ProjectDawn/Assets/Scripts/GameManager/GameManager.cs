using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    [Header("API Settings")]
    public string serverBaseUrl = "https://localhost:7123";

    [Header("Game State")]
    public string farmId;
    [SerializeField] private int playerId = 1;
    public TMP_InputField playerIdInput;  // assign in Inspector

    [Header("Object Prefabs")]
    public List<ObjectPrefabMapping> objectPrefabs = new List<ObjectPrefabMapping>();
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    [Header("Component References")]
    public ProjectDawnApi realTimeClient;

    public GameObject Menu;
    public GameObject Joystick;
    public Button SettingsButton;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (FindObjectsOfType<GameManager>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        foreach (var mapping in objectPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.typeKey))
                prefabDictionary[mapping.typeKey] = mapping.prefab;
        }
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

        if (Joystick != null)
            Joystick.SetActive(isActive);
    }

    public async Task<bool> JoinFarm(string newFarmId)
    {
        if (!string.IsNullOrEmpty(farmId))
        {
            Debug.Log($"[GameManager] Already connected to farm {farmId}. Leaving before joining {newFarmId}...");
            ResetToMenu();
        }

        Debug.Log($"[GameManager] Joining farm: {newFarmId}");
        farmId = newFarmId;

        string url = $"{serverBaseUrl}/api/Farms/{farmId}";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificate();
            await webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = webRequest.downloadHandler.text;
                FarmState farmData = JsonConvert.DeserializeObject<FarmState>(jsonResponse);
                BuildFarm(farmData);

                if (realTimeClient != null)
                {
                    bool success = await realTimeClient.ConnectAndJoin(serverBaseUrl, farmId, playerId);
                    if (success)
                    {
                        Debug.Log("[GameManager] Successfully joined farm.");
                        return true;
                    }
                    else
                    {
                        Debug.LogError("[GameManager] Failed to join farm (server rejected or connection issue).");
                        ResetToMenu(); // instead of farmId=null + ClearFarm()
                        return false;
                    }
                }
                else
                {
                    Debug.LogError("RealTimeClient (ProjectDawnApi) is not assigned!");
                    ResetToMenu();
                    return false;
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch farm state: {webRequest.error}");
                ResetToMenu();
                return false;
            }
        }
    }


    public void ResetToMenu()
    {
        Debug.Log("[GameManager] Resetting game state to menu...");

        // Clear farm objects + disconnect networking
        LeaveFarm();

        // Show farm selection again
        var farmUI = FindObjectOfType<FarmListUI>();
        if (farmUI != null)
            farmUI.gameObject.SetActive(true);

        // Hide joystick if it exists
        if (Joystick != null)
            Joystick.SetActive(false);

        // Reset farmId just to be safe
        farmId = null;
    }



    private IEnumerator LoadFarmStateAndConnect()
    {
        string url = $"{serverBaseUrl}/api/Farms/{farmId}";
        Debug.Log($"Requesting farm state from: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully fetched farm state.");
                string jsonResponse = webRequest.downloadHandler.text;
                FarmState farmData = JsonConvert.DeserializeObject<FarmState>(jsonResponse);

                BuildFarm(farmData);

                if (realTimeClient != null)
                    realTimeClient.ConnectAndJoin(serverBaseUrl, farmId, playerId);
                else
                    Debug.LogError("RealTimeClient (ProjectDawnApi) is not assigned!");
            }
            else
            {
                Debug.LogError($"Failed to fetch farm state: {webRequest.error}");
            }
        }
    }
    public async void LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving current farm...");

        // ✅ Clear farm objects
        ClearFarm();

        // ✅ Disconnect networking
        if (realTimeClient != null)
            realTimeClient.Disconnect();

        // ✅ Reset state
        farmId = null;
    }

    public void ForceLeaveFarmImmediate()
    {
        Debug.Log("[GameManager] Force leaving farm immediately...");

        // Clear players instantly
        if (realTimeClient != null && realTimeClient.playerManager != null)
        {
            realTimeClient.playerManager.ClearAllPlayers();
        }

        // Clear farm objects instantly
        ClearFarm();

        // Reset state
        farmId = null;

        // Reset UI
        if (Menu != null) Menu.SetActive(true);
        if (Joystick != null) Joystick.SetActive(false);

        // ❌ DO NOT call StopAsync here!
        // Let ProjectDawnApi.StopConnectionOnly() run separately if needed
    }




    private void ClearFarm()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    private void BuildFarm(FarmState farmData)
    {
        ClearFarm();

        foreach (var placedObject in farmData.placedObjects)
        {
            if (prefabDictionary.TryGetValue(placedObject.type, out GameObject prefab))
            {
                Vector3 position = new Vector3(
                    placedObject.transformation.positionX,
                    placedObject.transformation.positionY,
                    placedObject.transformation.positionZ
                );
                Quaternion rotation = Quaternion.Euler(
                    placedObject.transformation.rotationX,
                    placedObject.transformation.rotationY,
                    placedObject.transformation.rotationZ
                );

                Instantiate(prefab, position, rotation, transform);
            }
            else
            {
                Debug.LogWarning($"No prefab found for object type: '{placedObject.type}'. Skipping.");
            }
        }
    }
}

[System.Serializable]
public class ObjectPrefabMapping
{
    public string typeKey;
    public GameObject prefab;
}

public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}
