using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LogInMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text title;
    public GameObject confirmPassword;
    public TMP_Text loginButtonText;
    public TMP_Text switchText;
    public TMP_Text switchButtonText;
    public Button switchButton;

    private bool isRegister = false;

    private void Awake()
    {
        // Ensure initial state is Login
        ApplyState();
        switchButton.onClick.AddListener(ToggleMode);
    }

    private void ToggleMode()
    {
        isRegister = !isRegister;
        ApplyState();
    }

    private void ApplyState()
    {
        // Toggle Confirm Password field
        confirmPassword.SetActive(isRegister);

        // Update title
        title.text = isRegister ? "Register" : "Login";

        // Update main action button text
        loginButtonText.text = isRegister ? "Register" : "Log In";

        // Update bottom hint text
        switchText.text = isRegister
            ? "Already have an account?"
            : "Don't have an account?";

        // Update bottom switch button text
        switchButtonText.text = isRegister
            ? "Login"
            : "Register";
    }
}
