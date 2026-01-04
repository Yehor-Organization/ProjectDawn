using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [Header("Screens")]
    [SerializeField] private GameObject loginMenuUI;

    [SerializeField] private GameObject menuUI;
    [SerializeField] private GameObject gameUI;
    [SerializeField] private GameObject profileMenuUI;

    private UIState currentState;

    private enum UIState
    {
        Login,
        Menu,
        Game,
        Profile
    }

    // =====================
    // PUBLIC API (CALLED BY AUTHSERVICE)
    // =====================
    public void ShowLogin()
    {
        Debug.Log("[UIManager] ShowLogin");
        SetState(UIState.Login);
    }

    public void ShowMenu()
    {
        Debug.Log("[UIManager] ShowMenu");
        SetState(UIState.Menu);
    }

    public void ShowGameUI()
    {
        Debug.Log("[UIManager] ShowGameUI");
        SetState(UIState.Game);
    }

    public void ToggleMenu()
    {
        if (currentState == UIState.Menu)
            ShowGameUI();
        else
            ShowMenu();
    }

    public void ToggleProfileMenu()
    {
        if (profileMenuUI != null)
            profileMenuUI.SetActive(!profileMenuUI.activeSelf);
    }

    private void Update()
    {
        if (currentState == UIState.Login)
            return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
        {
            ToggleMenu();
        }
    }

    // =====================
    // INTERNAL
    // =====================

    private void SetState(UIState state)
    {
        currentState = state;

        loginMenuUI.SetActive(state == UIState.Login);
        menuUI.SetActive(state == UIState.Menu);
        gameUI.SetActive(state == UIState.Game);
        profileMenuUI.SetActive(state == UIState.Profile);

        Debug.Log($"[UIManager] UI State → {state}");
    }
}