using UnityEngine;
using UnityEngine.UI;
using System;

public class CameraUIController : MonoBehaviour
{
    private CameraManager cameraManager;

    [Header("UI Buttons")]
    [SerializeField] private Button rotateLeftButton;

    [SerializeField] private Button rotateRightButton;

    // -----------------------
    // Lazy dependency
    // -----------------------
    private CameraManager CameraManager
    {
        get
        {
            if (cameraManager == null)
                cameraManager = Core.Instance?.Managers?.CameraManager;

            if (cameraManager == null)
                throw new InvalidOperationException(
                    "[CameraUIController] CameraManager not available");

            return cameraManager;
        }
    }

    public void OnRotateLeftButton()
    {
        CameraManager.RotateAroundPlayerLeft();
    }

    public void OnRotateRightButton()
    {
        CameraManager.RotateAroundPlayerRight();
    }

    private void Awake()
    {
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.AddListener(OnRotateLeftButton);

        if (rotateRightButton != null)
            rotateRightButton.onClick.AddListener(OnRotateRightButton);
    }

    private void OnDestroy()
    {
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.RemoveListener(OnRotateLeftButton);

        if (rotateRightButton != null)
            rotateRightButton.onClick.RemoveListener(OnRotateRightButton);
    }
}