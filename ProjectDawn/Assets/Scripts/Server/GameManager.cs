using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class GameManager : MonoBehaviour
{
    [Header("API Settings")]
    public string serverBaseUrl = "https://localhost:7123";

    [Header("Game State")]
    public string farmId;
    public int playerId = 101;

    [Header("Object Prefabs")]
    public List<ObjectPrefabMapping> objectPrefabs = new List<ObjectPrefabMapping>();
    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    [Header("Component References")]
    public ProjectDawnApi realTimeClient;

    void Awake()
    {
        foreach (var mapping in objectPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.typeKey))
                prefabDictionary[mapping.typeKey] = mapping.prefab;
        }
    }

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
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Successfully fetched farm state.");
                string jsonResponse = webRequest.downloadHandler.text;
                FarmState farmData = JsonConvert.DeserializeObject<FarmState>(jsonResponse);

                BuildWorld(farmData);

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

    public void LeaveFarm()
    {
        Debug.Log("[GameManager] Leaving current farm...");

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        if (realTimeClient != null)
            realTimeClient.Disconnect();

        farmId = null;
    }

    private void BuildWorld(FarmState farmData)
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);

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
