using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FarmListItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text farmNameText;
    [SerializeField] private TMP_Text ownerNameText;
    [SerializeField] private Button joinButton;

    private string farmId;
    private GameManager gameManager;
    private FarmListUI farmListUI; // ✅ reference to parent

    public void Setup(FarmInfo farm, GameManager gm, FarmListUI parentUI)
    {
        farmId = farm.id.ToString();
        gameManager = gm;
        farmListUI = parentUI; // ✅ assign parent

        farmNameText.text = farm.name;
        ownerNameText.text = $"Owner: {farm.ownerName}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(JoinFarm);
    }

    private void JoinFarm()
    {
        Debug.Log($"[DEBUG][FarmListItemUI] Joining farm {farmId}...");
        gameManager.JoinFarm(farmId);

        // ✅ Tell the parent UI that a farm was joined
        farmListUI.FarmJoined();
    }
}
