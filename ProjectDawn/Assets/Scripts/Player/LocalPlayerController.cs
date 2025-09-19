using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 200f;
    public bool instantRotation = false; // true = snap instantly, false = smooth turning

    [Header("Momentum Settings")]
    [Range(0.01f, 1f)]
    [SerializeField] private float movementInertia = 0.1f; // lower = heavier inertia, higher = snappy
    [Range(0.01f, 1f)]
    [SerializeField] private float rotationInertia = 0.15f; // lower = snappy, higher = sluggish

    [Header("Camera")]
    private Vector3 cameraOffset;

    [Header("Networking")]
    public float positionUpdateThreshold = 0.05f;
    public float rotationUpdateThreshold = 1f;

    private FixedJoystick joystick;
    private ProjectDawnApi networkClient;
    private Vector3 lastPosition;
    private Vector3 lastRotation;

    private Rigidbody rb;

    // --- Momentum state ---
    private Vector3 currentVelocity; // movement momentum
    private float rotationVelocity;  // rotation momentum

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

        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (Terrain.activeTerrain != null)
        {
            float groundY = Terrain.activeTerrain.SampleHeight(transform.position);
            transform.position = new Vector3(transform.position.x, groundY + 1f, transform.position.z);
        }

        joystick = FindObjectOfType<FixedJoystick>();
        networkClient = FindObjectOfType<ProjectDawnApi>();

        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;
    }

    public void SetCamera(Camera cam, Vector3 offset)
    {
        cameraOffset = offset;
    }

    async void FixedUpdate()
    {
        // --- Build camera-relative movement axes ---
        Camera cam = CameraManager.Instance != null ? CameraManager.Instance.GetCamera() : Camera.main;
        Vector3 camForward = cam != null ? cam.transform.forward : Vector3.forward;
        Vector3 camRight = cam != null ? cam.transform.right : Vector3.right;

        // Flatten to XZ plane
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // --- Keyboard input ---
        Vector3 inputDir = Vector3.zero;
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) inputDir += camForward;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) inputDir -= camForward;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) inputDir -= camRight;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) inputDir += camRight;
        }

        if (inputDir.sqrMagnitude > 1f)
            inputDir.Normalize();

        // --- Joystick input ---
        Vector3 joystickDir = Vector3.zero;
        if (joystick != null)
            joystickDir = (camForward * joystick.Vertical) + (camRight * joystick.Horizontal);

        // --- Final move direction (joystick has priority) ---
        Vector3 moveDir = joystickDir.magnitude > 0.1f ? joystickDir : inputDir;

        // --- Apply movement & rotation ---
        if (moveDir.magnitude > 0.1f)
        {
            ApplyRotationMomentum(moveDir);
            ApplyMovementMomentum(moveDir.normalized);
        }
        else
        {
            ApplyMovementMomentum(Vector3.zero);
        }

        // --- Networking update ---
        bool moved = Vector3.Distance(transform.position, lastPosition) > positionUpdateThreshold;
        bool rotated = Vector3.Distance(transform.rotation.eulerAngles, lastRotation) > rotationUpdateThreshold;

        if (moved || rotated)
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation.eulerAngles;

            var transformation = new TransformationDC
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

    private void ApplyRotationMomentum(Vector3 moveDir)
    {
        if (instantRotation)
        {
            rb.MoveRotation(Quaternion.LookRotation(moveDir));
            return;
        }

        if (moveDir.sqrMagnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;

        float angle = Mathf.SmoothDampAngle(
            rb.rotation.eulerAngles.y,
            targetAngle,
            ref rotationVelocity,
            rotationInertia
        );

        rb.MoveRotation(Quaternion.Euler(0, angle, 0));
    }

    private void ApplyMovementMomentum(Vector3 moveDir)
    {
        Vector3 targetVelocity = moveDir * moveSpeed;
        targetVelocity.y = rb.linearVelocity.y;

        currentVelocity = Vector3.Lerp(
            rb.linearVelocity,
            targetVelocity,
            movementInertia
        );

        rb.linearVelocity = currentVelocity;
    }
}
