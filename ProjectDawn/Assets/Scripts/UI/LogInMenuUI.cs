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
    // Unity lifecycle
    // =====================

    private void Awake()
    {
        errorText.text = "";
        ApplyState();

        switchButton.onClick.AddListener(ToggleMode);
        loginButton.onClick.AddListener(OnSubmit);

        // 🔑 Trigger lazy auth initialization (UI handled by AuthService)
        _ = authService.GetValidAccessToken();
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
            // ❗ AuthService already handled UI state
            // We only show local error
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