using System.Threading.Tasks;
using UnityEngine;

public class Core : MonoBehaviour
{
    [Header("Containers")]
    public ApiCommunicators ApiCommunicators;

    public Managers Managers;
    public Services Services;

    private static TaskCompletionSource<Core> readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Core Instance { get; private set; }

    public static Task<Core> WhenReady => readyTcs.Task;

    private void Awake()
    {
        Application.runInBackground = true;

        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Validate();
        readyTcs.TrySetResult(this);
    }

    private void Validate()
    {
        if (Managers == null)
            Debug.LogError("[Core] Managers container not assigned");

        if (Services == null)
            Debug.LogError("[Core] Services container not assigned");

        if (ApiCommunicators == null)
            Debug.LogError("[Core] ApiCommunicators container not assigned");
    }
}