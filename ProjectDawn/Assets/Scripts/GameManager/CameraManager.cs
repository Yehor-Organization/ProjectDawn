using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Header("Camera Reference")]
    [SerializeField] private Camera mainCam;

    private LocalPlayerController target;
    private Vector3 offset;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetCamera(LocalPlayerController ctrl)
    {
        if (mainCam == null)
        {
            Debug.LogError("[CameraManager] No camera assigned!");
            return;
        }

        // snap back to original editor pose
        mainCam.transform.position = initialPosition;
        mainCam.transform.rotation = initialRotation;

        // calculate and store offset
        offset = mainCam.transform.position - ctrl.transform.position;
        ctrl.SetCamera(mainCam, offset);

        target = ctrl;
    }

    void LateUpdate()
    {
        if (mainCam != null && target != null)
        {
            mainCam.transform.position = target.transform.position + offset;
            mainCam.transform.LookAt(target.transform.position);
        }
    }
}
