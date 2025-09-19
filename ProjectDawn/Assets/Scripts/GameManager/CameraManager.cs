using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Camera Reference")]
    [SerializeField]
    private Camera mainCam;

    private LocalPlayerController localPlayerController;
    private Vector3 offset;
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

    public Camera GetCamera()
    {
        return mainCam;
    }

    public void ResetCamera(LocalPlayerController ctrl)
    {
        if (mainCam == null)
        {
            Debug.LogError("[CameraManager] No camera assigned!");
            return;
        }

        mainCam.transform.position = initialPosition;
        mainCam.transform.rotation = initialRotation;

        offset = mainCam.transform.position - ctrl.transform.position;
        ctrl.SetCamera(mainCam, offset);

        localPlayerController = ctrl;
    }

    void LateUpdate()
    {
        if (mainCam != null && localPlayerController != null)
        {
            mainCam.transform.position = localPlayerController.transform.position + offset;
            mainCam.transform.LookAt(localPlayerController.transform.position);
        }
    }
}
