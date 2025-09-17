using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 200f;
    public bool instantRotation = false; // true = snap instantly, false = smooth turning

    [Header("Camera")]
    private Vector3 cameraOffset;
    private Camera playerCamera;

    [Header("Networking")]
    public float positionUpdateThreshold = 0.05f;
    public float rotationUpdateThreshold = 1f;

    private FixedJoystick joystick;
    private ProjectDawnApi networkClient;
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private Rigidbody rb;

    /// <summary>
    /// Configures movement/rotation thresholds for this player.
    /// </summary>
    public void Initialize(float moveSpeed, float rotateSpeed, float positionUpdateThreshold, float rotationUpdateThreshold)
    {
        this.moveSpeed = moveSpeed;
        this.rotateSpeed = rotateSpeed;
        this.positionUpdateThreshold = positionUpdateThreshold;
        this.rotationUpdateThreshold = rotationUpdateThreshold;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // ✅ Prevent physics spin, allow collisions to stop movement
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            cameraOffset = playerCamera.transform.position - transform.position;
            playerCamera.transform.LookAt(transform.position);
        }

        joystick = FindObjectOfType<FixedJoystick>();
        networkClient = FindObjectOfType<ProjectDawnApi>();

        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;
    }

    async void FixedUpdate()
    {
        Vector3 inputDir = Vector3.zero;

        // --- Keyboard input as direction ---
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDir += Vector3.forward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDir += Vector3.back;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDir += Vector3.left;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDir += Vector3.right;
        }

        // Normalize keyboard vector
        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        // --- Joystick input ---
        Vector3 joystickDir = Vector3.zero;
        if (joystick != null)
        {
            joystickDir = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        }

        // Prefer joystick if active
        Vector3 moveDir = joystickDir.magnitude > 0.1f ? joystickDir : inputDir;

        // --- Movement & Rotation ---
        if (moveDir.magnitude > 0.1f)
        {
            Vector3 worldDir = new Vector3(moveDir.x, 0, moveDir.z).normalized;

            // Rotate
            Quaternion targetRotation = Quaternion.LookRotation(worldDir);
            if (instantRotation)
                rb.MoveRotation(targetRotation);
            else
                rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, rotateSpeed * Time.fixedDeltaTime));

            // ✅ Use velocity for proper collisions
            rb.linearVelocity = new Vector3(worldDir.x * moveSpeed, rb.linearVelocity.y, worldDir.z * moveSpeed);
        }
        else
        {
            // Idle → keep gravity
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // --- Networking Sync ---
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

            if (networkClient != null)
                await networkClient.SendTransformationUpdate(transformation);
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
