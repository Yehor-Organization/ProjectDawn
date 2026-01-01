using UnityEngine;

public class Core : MonoBehaviour
{
    public ApiCommunicators ApiCommunicators;

    public Managers Managers;

    public Services Services;
    public static Core Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Validate();
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