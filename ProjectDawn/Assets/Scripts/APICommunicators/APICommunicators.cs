using System.Threading.Tasks;
using UnityEngine;

public class ApiCommunicators : MonoBehaviour
{
    public AuthAPICommunicator AuthApi;
    public FarmAPICommunicator FarmApi;
    public FarmHubCommunicator FarmHub;
    public FarmListHubCommunicator FarmListHub;
    public ObjectPlacementCommunicator ObjectPlacement;
    public PlayerMovementCommunicator PlayerMovement;

    private TaskCompletionSource<ApiCommunicators> readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WhenReady => readyTcs.Task;

    private void Awake()
    {
        readyTcs.TrySetResult(this);
    }
}