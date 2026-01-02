using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject loginMenuUI;

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject profileMenuUI;

    [Header("Auth")]
    [SerializeField] private AuthService authService;

    private UIState currentState;

    private enum UIState
    {
        Login,
        Menu,
        Game
    }

    // =====================
    // Unity lifecycle
    // =====================

    public void ShowLogin()
    {
        HideAllOverlays();
        SetState(UIState.Login);
    }

    // =====================
    // Primary Screens
    // =====================
    public void ShowMenu()
    {
        HideAllOverlays();
        SetState(UIState.Menu);
    }

    public void ShowGameUI()
    {
        HideAllOverlays();
        SetState(UIState.Game);
    }

    public void ToggleMenu()
    {
        Debug.Log("ToggleMenu");

        bool isMenuActive = menuUI.activeSelf;
        bool isGameActive = gameUI.activeSelf;

        if (isMenuActive)
            ShowGameUI();
        else if (isGameActive)
            ShowMenu();
    }

    public void ShowProfileMenu()
    {
        if (profileMenuUI == null)
        {
            Debug.LogWarning("[UIManager] ProfileMenu not assigned");
            return;
        }

        profileMenuUI.SetActive(true);
    }

    // =====================
    // Profile Menu (Overlay)
    // =====================
    public void HideProfileMenu()
    {
        if (profileMenuUI == null)
            return;

        profileMenuUI.SetActive(false);
    }

    public void ToggleProfileMenu()
    {
        if (profileMenuUI == null)
            return;

        profileMenuUI.SetActive(!profileMenuUI.activeSelf);
    }

    private void Awake()
    {
        if (authService == null)
        {
            Debug.LogError("[UIManager] AuthService not assigned");
            return;
        }

        authService.Authenticated += OnAuthenticated;
        authService.AuthInvalidated += OnAuthInvalidated;
    }

    private void OnDestroy()
    {
        if (authService == null)
            return;

        authService.Authenticated -= OnAuthenticated;
        authService.AuthInvalidated -= OnAuthInvalidated;
    }

    // =====================
    // Auth handlers
    // =====================

    private void OnAuthenticated()
    {
        Debug.Log("[UIManager] Authenticated → ShowMenu");
        ShowMenu();
    }

    private void OnAuthInvalidated(AuthInvalidReason reason)
    {
        Debug.Log($"[UIManager] AuthInvalidated ({reason}) → ShowLogin");
        ShowLogin();
    }

    private void HideAllOverlays()
    {
        if (profileMenuUI != null)
            profileMenuUI.SetActive(false);
    }

    // =====================
    // Internal
    // =====================

    private void SetState(UIState state)
    {
        currentState = state;

        loginMenuUI.SetActive(state == UIState.Login);
        menuUI.SetActive(state == UIState.Menu);
        gameUI.SetActive(state == UIState.Game);

        Debug.Log($"[UIManager] UI State set to {state}");
    }
}