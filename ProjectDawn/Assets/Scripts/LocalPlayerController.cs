using UnityEngine;
using UnityEngine.InputSystem;

public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    private float moveSpeed;
    private float rotateSpeed;

    [Header("Camera")]
    private Vector3 cameraOffset;
    private Camera playerCamera;

    [Header("Networking")]
    private float positionUpdateThreshold;
    private float rotationUpdateThreshold;

    private FixedJoystick joystick;
    private ProjectDawnApi networkClient;
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    /// <summary>
    /// Called by PlayerManager after spawning to configure speeds & thresholds.
    /// </summary>
    public void Initialize(float moveSpeed, float rotateSpeed, float positionUpdateThreshold, float protationUpdateThreshold)
    {
        this.moveSpeed = moveSpeed;
        this.rotateSpeed = rotateSpeed;
        this.positionUpdateThreshold = positionUpdateThreshold;
        this.rotationUpdateThreshold = rotationUpdateThreshold;

    }

    void Start()
    {
        // Setup camera
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            // Auto-calculate offset based on how the camera is placed in the editor
            cameraOffset = playerCamera.transform.position - transform.position;
            playerCamera.transform.LookAt(transform.position);
        }

        // Joystick lookup
        joystick = FindObjectOfType<FixedJoystick>();
        if (joystick == null)
            Debug.LogWarning("No joystick found in scene!");

        // Network client lookup
        networkClient = FindObjectOfType<ProjectDawnApi>();
        if (networkClient == null)
            Debug.LogError("Could not find ProjectDawnApi script in the scene!");

        lastPosition = transform.position;
    }

    void Update()
    {
        float h = 0f;
        float v = 0f;

        // --- Keyboard input ---
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) v += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) v -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) h -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) h += 1f;
        }

        // --- Joystick input ---
        Vector3 joystickDir = Vector3.zero;
        if (joystick != null)
        {
            joystickDir = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
            if (joystickDir.magnitude > 0.1f)
            {
                transform.forward = joystickDir.normalized;  // snap forward to joystick direction
                v = joystickDir.magnitude;
                h = 0f;
            }
        }

        // --- Apply Movement ---
        transform.position += transform.forward * v * moveSpeed * Time.deltaTime;

        if (h != 0f && joystickDir.magnitude <= 0.1f)
        {
            transform.Rotate(Vector3.up * h * rotateSpeed * Time.deltaTime);
        }

        // --- Send Transformation Update ---
        bool moved = Vector3.Distance(transform.position, lastPosition) > positionUpdateThreshold;
        bool rotated = Vector3.Distance(transform.rotation.eulerAngles, lastRotation) > rotationUpdateThreshold;

        if (moved || rotated)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation.eulerAngles;

            var transformation = new TransformationDataModel
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                positionZ = transform.position.z,
                rotationX = transform.rotation.eulerAngles.x,
                rotationY = transform.rotation.eulerAngles.y,
                rotationZ = transform.rotation.eulerAngles.z
            };

            networkClient.SendTransformationUpdate(transformation);
        }
    }

    void LateUpdate()
    {
        if (playerCamera != null)
        {
            playerCamera.transform.position = transform.position + cameraOffset;
            playerCamera.transform.LookAt(transform.position);
        }
    }
}
