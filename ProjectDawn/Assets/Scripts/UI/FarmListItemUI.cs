using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class FarmListItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text farmNameText;

    [SerializeField] private TMP_Text ownerNameText;
    [SerializeField] private TMP_Text visitorCountText;
    [SerializeField] private Button joinButton;

    private int farmId;
    private GameManager gameManager;
    private FarmListUI farmListUI;

    private bool joinInProgress;

    // =======================
    // Setup
    // =======================

    public void Setup(FarmInfoDTO farm, GameManager gm, FarmListUI parentUI)
    {
        if (farm == null)
        {
            Debug.LogError("[FarmListItemUI] Setup called with null farm");
            return;
        }

        farmId = farm.Id;
        gameManager = gm;
        farmListUI = parentUI;

        farmNameText.text = farm.Name;
        ownerNameText.text = $"Owner: {farm.OwnerName}";
        visitorCountText.text = $"Visitors: {farm.VisitorCount}";

        joinInProgress = false;

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(OnJoinClicked);
        joinButton.interactable = true;
    }

    // =======================
    // Join logic
    // =======================

    private void OnJoinClicked()
    {
        if (joinInProgress)
            return;

        _ = JoinFarmAsync();
    }

    private async Task JoinFarmAsync()
    {
        if (gameManager == null)
        {
            Debug.LogError("[FarmListItemUI] GameManager missing");
            return;
        }

        joinInProgress = true;
        joinButton.interactable = false;

        Debug.Log($"[FarmListItemUI] Joining farm {farmId}...");

        bool joinedSuccessfully = false;

        try
        {
            joinedSuccessfully = await gameManager.JoinFarm(farmId.ToString());
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[FarmListItemUI] JoinFarm failed: {ex}");
        }

        // Object might be destroyed while awaiting
        if (!this || !gameObject.activeInHierarchy)
            return;

        if (joinedSuccessfully)
        {
            Core.Instance?.Managers?.UIManager?.ShowGameUI();
        }
        else
        {
            joinButton.interactable = true;
            joinInProgress = false;
        }
    }
}