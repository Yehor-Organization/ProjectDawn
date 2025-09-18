using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text farmNameText;
    [SerializeField] private TMP_Text ownerNameText;
    [SerializeField] private TMP_Text visitorCountText;
    [SerializeField] private Button joinButton;

    private string farmId;
    private GameManager gameManager;
    private FarmListUI farmListUI; // ✅ reference to parent

    public void Setup(FarmInfoDto farm, GameManager gm, FarmListUI parentUI)
    {
        farmId = farm.id.ToString();
        gameManager = gm;
        farmListUI = parentUI; // ✅ assign parent

        farmNameText.text = farm.name;
        ownerNameText.text = $"Owner: {farm.ownerName}";

        int visitors = (farm.visitors != null) ? farm.visitors.Count : 0;
        visitorCountText.text = $"Visitors: {visitors}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(JoinFarm);
    }

    private async void JoinFarm()
    {
        Debug.Log($"[DEBUG][FarmListItemUI] Joining farm {farmId}...");
        bool joinedSuccessfully = await gameManager.JoinFarm(farmId);

        if (joinedSuccessfully)
        {
            farmListUI.FarmJoined();
        }
    }
}
