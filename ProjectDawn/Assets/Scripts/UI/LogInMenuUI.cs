using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LogInMenuUI : MonoBehaviour
{
    // =====================
    // Input Fields
    // =====================

    private AuthService authService;

    [SerializeField] private TMP_InputField confirmPasswordInput;

    [SerializeField] private TMP_Text errorText;

    private bool isBusy;

    // =====================
    // Internal state
    // =====================
    private bool isRegister;

    [Header("Buttons")]
    [SerializeField] private Button loginButton;

    [SerializeField] private TMP_Text loginButtonText;

    [SerializeField] private TMP_InputField passwordInput;

    // =====================
    // Buttons
    // =====================
    [SerializeField] private Button switchButton;

    [SerializeField] private TMP_Text switchButtonText;

    [SerializeField] private TMP_Text switchText;

    [Header("Text")]
    [SerializeField] private TMP_Text title;

    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameInput;

    // =====================
    // Text
    // =====================
    // =====================
    // Unity lifecycle
    // =====================

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
        errorText.text = "";
        ApplyState();

        switchButton.onClick.AddListener(ToggleMode);
        loginButton.onClick.AddListener(() => _ = OnSubmitAsync());

        _ = AwakeAsync();
    }

    private async Task AwakeAsync()
    {
        try
        {
            Debug.Log("[LoginUI] Waiting for AuthService");

            await EnsureAuthServiceAsync();

            // Warm up auth safely (no deadlock, no swallow)
            _ = WarmupAuthAsync();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            errorText.text = ex.Message;
            SetBusy(false);
        }
    }

    private void ClearInputs()
    {
        usernameInput.text = "";
        passwordInput.text = "";
        confirmPasswordInput.text = "";
    }

    private async Task EnsureAuthServiceAsync()
    {
        if (authService != null)
            return;

        authService = await CoreWaitHelpers.WaitForServiceAsync(s => s.AuthService);
    }

    private async Task OnSubmitAsync()
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
            Debug.LogException(ex);
            errorText.text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    // =====================
    // Helpers
    // =====================
    private void SetBusy(bool busy)
    {
        isBusy = busy;
        loginButton.interactable = !busy;
        switchButton.interactable = !busy;
    }

    // =====================
    // UI behavior
    // =====================
    private void ToggleMode()
    {
        if (isBusy)
            return;

        isRegister = !isRegister;
        ApplyState();
        ClearInputs();
        errorText.text = "";
    }

    private async Task WarmupAuthAsync()
    {
        try
        {
            await authService.GetValidAccessToken();
            Debug.Log("[LoginUI] Auth warmup complete");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            // Optional: show non-fatal message
            // errorText.text = "Authentication unavailable.";
        }
    }
}