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
    [Tooltip("The ID of the farm to load.")]
    public string farmId = "1";
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

    void Start()
    {
        // Start the process of loading the farm
        StartCoroutine(LoadFarmState());
    }

    private IEnumerator LoadFarmState()
    {
        string url = $"{serverBaseUrl}/api/Farms/{farmId}";
        Debug.Log($"Requesting farm state from: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // --- HACK FOR DEVELOPMENT ONLY ---
            // This bypasses SSL certificate validation.
            // Do NOT use this in a production build.
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
                    // ***** THIS IS THE CORRECTED LINE *****
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

    private void BuildWorld(FarmState farmData)
    {
        Debug.Log($"Spawning {farmData.placedObjects.Count} objects for farm '{farmData.name}'...");
        foreach (var placedObject in farmData.placedObjects)
        {
            if (prefabDictionary.TryGetValue(placedObject.type, out GameObject prefab))
            {
                Vector3 position = new Vector3(placedObject.position.x, placedObject.position.y, placedObject.position.z);
                Quaternion rotation = Quaternion.Euler(0, placedObject.rotationY, 0);

                Instantiate(prefab, position, rotation);
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
// This is a helper class to bypass SSL certificate errors when using 'localhost'
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        //Simply return true no matter what
        return true;
    }
}

