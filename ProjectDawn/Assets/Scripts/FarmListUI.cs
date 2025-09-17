using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class FarmListUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RectTransform farmListContainer;
    [SerializeField] private GameObject farmListItemPrefab;
    [SerializeField] private GameObject joystickUI;
    [SerializeField] private float refreshPeroid = 10f;

    void Start()
    {
        joystickUI.SetActive(false);
    }

    void OnEnable()
    {
        Debug.Log("[DEBUG][FarmListUI] OnEnable → starting periodic farm fetch...");
        StartCoroutine(FetchFarmsPeriodically(refreshPeroid));
    }

    private IEnumerator FetchFarmsPeriodically(float interval)
    {
        while (true)
        {
            yield return FetchFarms(); 
            yield return new WaitForSeconds(interval); 
        }
    }

    private IEnumerator FetchFarms()
    {
        string url = $"{gameManager.serverBaseUrl}/api/Farms";
        Debug.Log($"[DEBUG][FarmListUI] Fetching farms from: {url}");

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.certificateHandler = new BypassCertificate();
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[DEBUG][FarmListUI] Failed to fetch farms → {webRequest.error}");
                yield break;
            }

            Debug.Log($"[DEBUG][FarmListUI] Response received: {webRequest.downloadHandler.text}");

            var farms = JsonConvert.DeserializeObject<List<FarmInfo>>(webRequest.downloadHandler.text);

            if (farms == null)
            {
                Debug.LogError("[DEBUG][FarmListUI] Deserialized farms list is NULL!");
                yield break;
            }

            Debug.Log($"[DEBUG][FarmListUI] Parsed {farms.Count} farms from API.");
            PopulateFarmList(farms);
        }
    }

    public void FarmJoined()
    {
        joystickUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private void PopulateFarmList(List<FarmInfo> farms)
    {
        Debug.Log("[DEBUG][FarmListUI] Clearing old farm list items...");
        foreach (Transform child in farmListContainer)
            Destroy(child.gameObject);

        Debug.Log($"[DEBUG][FarmListUI] Populating UI with {farms.Count} farms...");
        foreach (var farm in farms)
        {
            int visitors = (farm.visitors != null) ? farm.visitors.Count : 0;
            Debug.Log($"[DEBUG][FarmListUI] Creating item for farmId={farm.id}, name={farm.name}, owner={farm.ownerName}, visitors={visitors}");

            var item = Instantiate(farmListItemPrefab, farmListContainer);
            var farmItemUI = item.GetComponent<FarmListItemUI>();
            farmItemUI.Setup(farm, gameManager, this);
        }

        Debug.Log("[DEBUG][FarmListUI] Farm list UI updated successfully.");
    }


}
