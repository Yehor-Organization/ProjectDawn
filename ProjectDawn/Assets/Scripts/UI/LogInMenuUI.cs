using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInMenuUI : MonoBehaviour
{
    // =========================
    // AUTH / SERVICES
    // =========================
    [Header("Auth / Services")]
    [SerializeField] private AuthService authService;

    // =========================
    // ROOT
    // =========================
    [Header("Root")]
    [SerializeField] private GameObject rootPanel;

    // =========================
    // INPUT FIELDS
    // =========================
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;

    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField confirmPasswordInput;

    // =========================
    // BUTTONS
    // =========================
    [Header("Buttons")]
    [SerializeField] private Button loginButton;

    [SerializeField] private Button switchButton;

    // =========================
    // TEXT
    // =========================
    [Header("Text")]
    [SerializeField] private TMP_Text title;

    [SerializeField] private TMP_Text loginButtonText;
    [SerializeField] private TMP_Text switchText;
    [SerializeField] private TMP_Text switchButtonText;
    [SerializeField] private TMP_Text errorText;

    // =========================
    // STATE
    // =========================
    private bool isBusy = false;

    private bool isRegister = false;

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

    private void Awake()
    {
        rootPanel.SetActive(false);
        errorText.text = "";

        ApplyState();

        switchButton.onClick.AddListener(ToggleMode);
        loginButton.onClick.AddListener(OnSubmit);

        authService.AuthInvalidated += OnAuthInvalidated;

        // Force initial auth check
        _ = authService.GetValidAccessToken();
    }

    private void ClearInputs()
    {
        usernameInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }

    private void OnAuthInvalidated(AuthInvalidReason reason)
    {
        rootPanel.SetActive(true);
        SetLoginMode();
        ClearInputs();

        errorText.text = reason switch
        {
            AuthInvalidReason.RefreshFailed => "Session expired. Please log in again.",
            AuthInvalidReason.TokenCorrupted => "Authentication error. Please log in.",
            _ => ""
        };
    }

    private void OnDestroy()
    {
        authService.AuthInvalidated -= OnAuthInvalidated;
    }

    private async void OnSubmit()
    {
        if (isBusy) return;

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

            rootPanel.SetActive(false);
            ClearInputs();
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

    // =========================
    // Submit
    // =========================
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        loginButton.interactable = !busy;
        switchButton.interactable = !busy;
    }

    private void SetLoginMode()
    {
        isRegister = false;
        ApplyState();
    }

    // =========================
    // Auth Event
    // =========================
    // =========================
    // UI Logic
    // =========================
    private void ToggleMode()
    {
        if (isBusy) return;

        isRegister = !isRegister;
        ApplyState();
        ClearInputs();
        errorText.text = "";
    }
}