using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json; // You will need the Json.NET for Unity package

/// <summary>
/// Orchestrates the game startup sequence.
/// 1. Fetches the initial farm state from the REST API.
/// 2. Spawns the static farm objects.
/// 3. Activates the SignalR client to handle real-time updates.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("The base URL of the REST API. Example: 'https://localhost:7123'")]
    public string serverBaseUrl = "https://localhost:7123";

    [Header("Game State")]
    [Tooltip("The ID of the farm to load. This can be set via UI.")]
    public string farmId;
    [Tooltip("The ID of the current player.")]
    public int playerId = 101;

    [Header("Object Prefabs")]
    [Tooltip("Assign prefabs for different object types. The 'Key' should match the 'type' string from the API.")]
    public List<ObjectPrefabMapping> objectPrefabs = new List<ObjectPrefabMapping>();
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    [Header("Component References")]
    [Tooltip("Drag the GameObject with the ProjectDawnApi script here.")]
    public ProjectDawnApi realTimeClient;

    void Awake()
    {
        // Convert the list to a dictionary for faster lookups
        foreach (var mapping in objectPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.typeKey))
            {
                prefabDictionary[mapping.typeKey] = mapping.prefab;
            }
        }
    }

    /// <summary>
    /// Called from UI (button) to start joining a farm.
    /// </summary>
    public void JoinFarm(string newFarmId)
    {
        farmId = newFarmId;
        StartCoroutine(LoadFarmStateAndConnect());
    }

    private IEnumerator LoadFarmStateAndConnect()
    {
        string url = $"{serverBaseUrl}/api/Farms/{farmId}";
        Debug.Log($"Requesting farm state from: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // --- HACK FOR DEVELOPMENT ONLY ---
            webRequest.certificateHandler = new BypassCertificate();
            // --------------------------------

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully fetched farm state.");
                string jsonResponse = webRequest.downloadHandler.text;

                // Deserialize the JSON into our data models
                FarmState farmData = JsonConvert.DeserializeObject<FarmState>(jsonResponse);

                // Now, build the world based on this data
                BuildWorld(farmData);

                // Finally, connect to the real-time hub
                Debug.Log("World built. Connecting to real-time service...");
                if (realTimeClient != null)
                {
                    realTimeClient.ConnectAndJoin(serverBaseUrl, farmId, playerId);
                }
                else
                {
                    Debug.LogError("RealTimeClient (ProjectDawnApi) is not assigned in the GameManager inspector!");
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch farm state: {webRequest.error}");
            }
        }
    }

    public void LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving current farm...");

        // Destroy spawned objects
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Disconnect real-time
        if (realTimeClient != null)
        {
            realTimeClient.Disconnect();
        }

        farmId = null;
    }


    private void BuildWorld(FarmState farmData)
    {
        // 🔹 Clear old objects (only those spawned by GameManager)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

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

                Instantiate(prefab, position, rotation, transform); // parent under GameManager
            }
            else
            {
                Debug.LogWarning($"No prefab found for object type: '{placedObject.type}'. Skipping.");
            }
        }
    }

}

// Helper class for the inspector
[System.Serializable]
public class ObjectPrefabMapping
{
    public string typeKey;
    public GameObject prefab;
}

// --- HACK FOR DEVELOPMENT ONLY ---
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) => true;
}
