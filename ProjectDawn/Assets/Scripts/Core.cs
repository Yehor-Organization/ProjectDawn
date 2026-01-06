using System.Threading.Tasks;
using UnityEngine;

public class Core : MonoBehaviour
{
    [Header("Containers")]
    public ApiCommunicators ApiCommunicators;
    public Managers Managers;
    public Services Services;

    [Header("Config")]
    [SerializeField] private AppConfig appConfig;

    public static Core Instance { get; private set; }

    // =========================
    // READINESS
    // =========================

    private static TaskCompletionSource<Core> readyTcs =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public static Task<Core> WhenReady => readyTcs.Task;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeConfig();
        Validate();

        readyTcs.TrySetResult(this);
    }

    private void InitializeConfig()
    {
        if (appConfig == null)
        {
            Debug.LogError("[Core] AppConfig not assigned");
            return;
        }

        Config.APIBaseUrl = appConfig.APIBaseUrl.TrimEnd('/');
        Debug.Log($"[Core] APIBaseUrl initialized: {Config.APIBaseUrl}");
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
