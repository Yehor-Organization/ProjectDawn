using UnityEngine;
using UnityEngine.UI;

public class CameraUIController : MonoBehaviour
{
    [Header("UI Buttons")]
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;

    void Awake()
    {
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.AddListener(OnRotateLeftButton);

        if (rotateRightButton != null)
            rotateRightButton.onClick.AddListener(OnRotateRightButton);
    }

    public void OnRotateLeftButton()
    {
        if (CameraManager.Instance != null)
            CameraManager.Instance.RotateAroundPlayerLeft();
    }

    public void OnRotateRightButton()
    {
        if (CameraManager.Instance != null)
            CameraManager.Instance.RotateAroundPlayerRight();
    }

    void OnDestroy()
    {
        // ✅ Clean up listeners to prevent memory leaks
        if (rotateLeftButton != null)
            rotateLeftButton.onClick.RemoveListener(OnRotateLeftButton);

        if (rotateRightButton != null)
            rotateRightButton.onClick.RemoveListener(OnRotateRightButton);
    }
}
