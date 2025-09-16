using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 200f;

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
    /// Called by PlayerManager after spawning to configure speeds & thresholds.
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
        // Rigidbody setup
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Camera
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            cameraOffset = playerCamera.transform.position - transform.position;
            playerCamera.transform.LookAt(transform.position);
        }

        // Joystick lookup
        joystick = FindObjectOfType<FixedJoystick>();

        // Network client lookup
        networkClient = FindObjectOfType<ProjectDawnApi>();

        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;
    }

    void FixedUpdate()
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
                // 🔥 Instant rotation toward joystick direction
                Quaternion targetRotation = Quaternion.LookRotation(joystickDir.normalized);
                rb.MoveRotation(targetRotation);

                v = joystickDir.magnitude;
                h = 0f; // disable keyboard rotation while joystick is active
            }
        }

        // --- Movement using velocity (safe collisions) ---
        if (v != 0)
        {
            rb.linearVelocity = transform.forward * v * moveSpeed + new Vector3(0, rb.linearVelocity.y, 0);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0); // stop horizontal motion, keep gravity
        }

        // --- Rotation from keyboard (if no joystick) ---
        if (h != 0f && joystickDir.magnitude <= 0.1f)
        {
            Quaternion turn = Quaternion.Euler(Vector3.up * h * rotateSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * turn);
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
