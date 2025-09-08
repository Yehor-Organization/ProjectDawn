using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Relay relay; // Reference to your Relay script
    [SerializeField] private TMP_InputField joinCodeInput;

private async void Awake()
    {
        // Host button creates a relay with a max player count
        hostButton.onClick.AddListener(async () =>
        {
            string joinCode = await relay.CreateRelay(3); // Change 3 to your desired max players
            joinCodeInput.text = joinCode; // Display the join code for clients to use  
        });

        // Client button reads join code from input field
        joinButton.onClick.AddListener(() =>
        {
            string joinCode = joinCodeInput.text.Trim();

            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogWarning("Please enter a valid join code!");
                return;
            }

            relay.JoinRelay(joinCode);
        });
    }
}
