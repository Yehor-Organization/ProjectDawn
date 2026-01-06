using System.Threading.Tasks;
using UnityEngine;

public class Managers : MonoBehaviour
{
    public CameraManager CameraManager;
    public GameManager GameManager;
    public InventoryManager InventoryManager;
    public ObjectManager ObjectManager;
    public PlayerManager PlayerManager;
    public UIManager UIManager;

    // =========================
    // READINESS
    // =========================

    private TaskCompletionSource<Managers> readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task WhenReady => readyTcs.Task;

    private void Awake()
    {
        readyTcs.TrySetResult(this);
    }
}
