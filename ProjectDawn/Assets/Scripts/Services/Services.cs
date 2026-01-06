using System.Threading.Tasks;
using UnityEngine;

public class Services : MonoBehaviour
{
    public AuthService AuthService;

    private TaskCompletionSource<Services> readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WhenReady => readyTcs.Task;

    private void Awake()
    {
        readyTcs.TrySetResult(this);
    }
}
