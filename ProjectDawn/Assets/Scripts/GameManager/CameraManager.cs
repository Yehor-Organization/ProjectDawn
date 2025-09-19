using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Rotation Settings")]
    [SerializeField] private float rotateStep = 90f;   // degrees per button press
    [SerializeField] private float rotateSpeed = 5f;   // how fast the camera rotates (higher = snappier)

    [Header("Camera Reference")]
    [SerializeField] private Camera mainCam;

    private LocalPlayerController localPlayerController;

    private Vector3 offset;         // current smoothed offset
    private Vector3 targetOffset;   // smooth target
    private Vector3 orbitOffsetXZ;  // ✅ persistent flat XZ offset for orbiting
    private float cameraHeight;     // ✅ fixed Y height

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);

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

    void Update()
    {
        // Use new Input System API
        if (Keyboard.current != null)
        {
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                RotateAroundPlayerLeft();
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                RotateAroundPlayerRight();
            }
        }
    }

    public void RotateAroundPlayerLeft() => SetTargetRotation(rotateStep);
    public void RotateAroundPlayerRight() => SetTargetRotation(-rotateStep);

    private void SetTargetRotation(float angleDegrees)
    {
        if (mainCam == null || localPlayerController == null) return;

        // Rotate only the flat XZ orbit vector
        orbitOffsetXZ = Quaternion.Euler(0f, angleDegrees, 0f) * orbitOffsetXZ;

        // Rebuild full offset using stored Y height
        targetOffset = new Vector3(orbitOffsetXZ.x, cameraHeight, orbitOffsetXZ.z);
    }

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

        ctrl.SetCamera(mainCam, offset);

        localPlayerController = ctrl;
    }

    void LateUpdate()
    {
        if (mainCam != null && localPlayerController != null)
        {
            // Smooth offset towards target
            offset = Vector3.Lerp(offset, targetOffset, Time.deltaTime * rotateSpeed);

            mainCam.transform.position = localPlayerController.transform.position + offset;
            mainCam.transform.LookAt(localPlayerController.transform.position);
        }
    }
}
