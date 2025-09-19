using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class FarmListUI : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private RectTransform farmListContainer;
    [SerializeField] private GameObject farmListItemPrefab;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private float refreshPeroid = 1f;

    void Start()
    {
        inGameUI.SetActive(false);
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
            yield return PopulateFarmListAsync(); 
            yield return new WaitForSeconds(interval); 
        }
    }


    public void FarmJoined()
    {
        inGameUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private async Task PopulateFarmListAsync()
    {
        List<FarmInfoDC> farms = await ProjectDawnApi.Instance.GetAllFarms();

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
