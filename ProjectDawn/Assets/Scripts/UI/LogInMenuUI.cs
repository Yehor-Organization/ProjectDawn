using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInMenuUI : MonoBehaviour
{
    // =====================
    // Auth / Services
    // =====================

    [Header("Auth / Services")]
    [SerializeField] private AuthService authService;

    // =====================
    // Input Fields
    // =====================

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;

    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    // =====================
    // Buttons
    // =====================

    [Header("Buttons")]
    [SerializeField] private Button loginButton;

    [SerializeField] private Button switchButton;

    // =====================
    // Text
    // =====================

    [Header("Text")]
    [SerializeField] private TMP_Text title;

    [SerializeField] private TMP_Text loginButtonText;
    [SerializeField] private TMP_Text switchText;
    [SerializeField] private TMP_Text switchButtonText;
    [SerializeField] private TMP_Text errorText;

    // =====================
    // Internal state
    // =====================

    private bool isBusy;
    private bool isRegister;

    // =====================
    // Lazy UIManager (SAFE)
    // =====================

    private UIManager uiManager;

    private UIManager UIManager
    {
        get
        {
            if (uiManager != null)
                return uiManager;

            if (Core.Instance == null || Core.Instance.Managers == null)
                return null;

            uiManager = Core.Instance.Managers.UIManager;
            return uiManager;
        }
    }

    // =====================
    // Unity lifecycle
    // =====================

    private void Awake()
    {
        errorText.text = "";
        ApplyState();

        switchButton.onClick.AddListener(ToggleMode);
        loginButton.onClick.AddListener(OnSubmit);

        authService.Authenticated += OnAuthenticated;
        authService.AuthInvalidated += OnAuthInvalidated;

        // Lazy auth check → may fire before UIManager exists (safe now)
        _ = authService.GetValidAccessToken();
    }

    private void OnDestroy()
    {
        if (authService == null)
            return;

        authService.Authenticated -= OnAuthenticated;
        authService.AuthInvalidated -= OnAuthInvalidated;
    }

    // =====================
    // Auth events
    // =====================

    private void OnAuthenticated()
    {
        ClearInputs();
        errorText.text = "";

        var manager = UIManager;
        if (manager == null)
        {
            Debug.LogWarning("[LogInMenuUI] UIManager not ready yet (Authenticated)");
            return;
        }

        manager.ShowMenu();
    }

    private void OnAuthInvalidated(AuthInvalidReason reason)
    {
        ClearInputs();
        SetLoginMode();

        errorText.text = reason switch
        {
            AuthInvalidReason.RefreshFailed => "Session expired. Please log in again.",
            AuthInvalidReason.TokenExpired => "Session expired. Please log in again.",
            AuthInvalidReason.TokenCorrupted => "Authentication error. Please log in.",
            _ => ""
        };

        var manager = UIManager;
        if (manager == null)
        {
            Debug.LogWarning("[LogInMenuUI] UIManager not ready yet (Invalidated)");
            return;
        }

        manager.ShowLogin();
    }

    // =====================
    // UI behavior
    // =====================

    private async void OnSubmit()
    {
        if (isBusy)
            return;

        errorText.text = "";

        string username = usernameInput.text.Trim();
        string password = passwordInput.text;
        string confirm = confirmPasswordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            errorText.text = "Username and password are required.";
            return;
        }

        if (isRegister && password != confirm)
        {
            errorText.text = "Passwords do not match.";
            return;
        }

        SetBusy(true);

        try
        {
            if (isRegister)
                await authService.Register(username, password);
            else
                await authService.Login(username, password);
        }
        catch (Exception ex)
        {
            errorText.text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ToggleMode()
    {
        if (isBusy)
            return;

        isRegister = !isRegister;
        ApplyState();
        ClearInputs();
        errorText.text = "";
    }

    private void ApplyState()
    {
        confirmPasswordInput.gameObject.SetActive(isRegister);

        title.text = isRegister ? "Register" : "Login";
        loginButtonText.text = isRegister ? "Register" : "Log In";

        switchText.text = isRegister
            ? "Already have an account?"
            : "Don't have an account?";

        switchButtonText.text = isRegister
            ? "Login"
            : "Register";
    }

    // =====================
    // Helpers
    // =====================

    private void SetLoginMode()
    {
        isRegister = false;
        ApplyState();
    }

    private void ClearInputs()
    {
        usernameInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;
        loginButton.interactable = !busy;
        switchButton.interactable = !busy;
    }
}