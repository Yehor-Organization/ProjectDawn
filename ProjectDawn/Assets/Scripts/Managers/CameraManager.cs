using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    private float cameraHeight;
    private Vector3 initialPosition;

    // ✅ fixed Y height
    private Quaternion initialRotation;

    private LocalPlayerController localPlayerController;

    [Header("Camera Reference")]
    [SerializeField] private Camera mainCam;

    private Vector3 offset;
    private Vector3 orbitOffsetXZ;
    [SerializeField] private float rotateSpeed = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotateStep = 90f;

    // degrees per button press
    // how fast the camera rotates (higher = snappier)
    // current smoothed offset
    private Vector3 targetOffset;

    public Camera GetCamera() => mainCam;

    public void ResetCamera(LocalPlayerController ctrl)
    {
        if (mainCam == null)
        {
            Debug.LogError("[CameraManager] No camera assigned!");
            return;
        }

        mainCam.transform.position = initialPosition;
        mainCam.transform.rotation = initialRotation;

        // Split the offset into flat XZ and height
        Vector3 fullOffset = mainCam.transform.position - ctrl.transform.position;
        cameraHeight = fullOffset.y;
        orbitOffsetXZ = new Vector3(fullOffset.x, 0f, fullOffset.z);

        offset = fullOffset;
        targetOffset = fullOffset;

        localPlayerController = ctrl;
    }

    public void RotateAroundPlayerLeft() => SetTargetRotation(rotateStep);

    public void RotateAroundPlayerRight() => SetTargetRotation(-rotateStep);

    // smooth target
    // ✅ persistent flat XZ offset for orbiting
    private void Awake()
    {
        if (mainCam != null)
        {
            initialPosition = mainCam.transform.position;
            initialRotation = mainCam.transform.rotation;
        }
        else
        {
            Debug.LogError("[CameraManager] Main camera is not assigned in Inspector!");
        }
    }

    private void LateUpdate()
    {
        if (mainCam != null && localPlayerController != null)
        {
            // Smooth offset towards target
            offset = Vector3.Lerp(offset, targetOffset, Time.deltaTime * rotateSpeed);

            mainCam.transform.position = localPlayerController.transform.position + offset;
            mainCam.transform.LookAt(localPlayerController.transform.position);
        }
    }

    private void SetTargetRotation(float angleDegrees)
    {
        if (mainCam == null || localPlayerController == null) return;

        // Rotate only the flat XZ orbit vector
        orbitOffsetXZ = Quaternion.Euler(0f, angleDegrees, 0f) * orbitOffsetXZ;

        // Rebuild full offset using stored Y height
        targetOffset = new Vector3(orbitOffsetXZ.x, cameraHeight, orbitOffsetXZ.z);
    }

    private void Update()
    {
        // 🔒 NEVER poll input while unfocused
        if (!Application.isFocused)
            return;

        var kb = Keyboard.current;
        if (kb == null)
            return;

        if (kb.qKey.wasPressedThisFrame)
        {
            RotateAroundPlayerLeft();
        }

        if (kb.eKey.wasPressedThisFrame)
        {
            RotateAroundPlayerRight();
        }
    }
}