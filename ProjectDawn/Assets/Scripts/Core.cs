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

    [Header("Config")]
    [SerializeField] private AppConfig appConfig;

    private float last;
    public static Core Instance { get; private set; }

    // =========================
    // READINESS
    // =========================
    public static Task<Core> WhenReady => readyTcs.Task;

    private void Awake()
    {
        // 🔥 CRITICAL: prevent focus-loss freezes
        Application.runInBackground = true;

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

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log($"[Core] Application focus: {hasFocus}");
    }

    private void Update()
    {
        if (!Application.isFocused)
            Debug.Log("[Core] Update running while unfocused");
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