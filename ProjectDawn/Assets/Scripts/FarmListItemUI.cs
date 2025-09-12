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

    public void Setup(FarmInfo farm, GameManager gm)
    {
        farmId = farm.id.ToString();
        gameManager = gm;

        farmNameText.text = farm.name;
        ownerNameText.text = $"Owner: {farm.ownerName}";

        joinButton.onClick.RemoveAllListeners();
        joinButton.onClick.AddListener(() => gameManager.JoinFarm(farmId));
    }
}
