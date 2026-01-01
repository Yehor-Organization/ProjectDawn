using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class LocalPlayerController : MonoBehaviour
{
    public bool instantRotation = false;

    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Networking")]
    public float positionUpdateThreshold = 0.05f;

    public float rotateSpeed = 200f;
    public float rotationUpdateThreshold = 1f;

    // 🔗 Dependencies (resolved once)
    private CameraManager cameraManager;

    private Vector3 currentVelocity;

    private FixedJoystick joystick;

    private Vector3 lastPosition;

    private Vector3 lastRotation;

    private float lastSendTime;

    private PlayerMovementCommunicator movementCommunicator;

    [Header("Momentum")]
    [Range(0.01f, 1f)][SerializeField] private float movementInertia = 0.1f;

    private Rigidbody rb;
    [Range(0.01f, 1f)][SerializeField] private float rotationInertia = 0.15f;
    private float rotationVelocity;
    [SerializeField] private float sendInterval = 0.1f;

    public void Initialize(
        float moveSpeed,
        float rotateSpeed,
        float posThreshold,
        float rotThreshold)
    {
        this.moveSpeed = moveSpeed;
        this.rotateSpeed = rotateSpeed;
        positionUpdateThreshold = posThreshold;
        rotationUpdateThreshold = rotThreshold;
    }

    private void ApplyMovement(Vector3 moveDir)
    {
        if (moveDir.sqrMagnitude > 0.01f)
        {
            ApplyRotation(moveDir);
            ApplyVelocity(moveDir.normalized);
        }
        else
        {
            ApplyVelocity(Vector3.zero);
        }
    }

    private void ApplyRotation(Vector3 moveDir)
    {
        if (instantRotation)
        {
            rb.MoveRotation(Quaternion.LookRotation(moveDir));
            return;
        }

        float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
        float angle = Mathf.SmoothDampAngle(
            rb.rotation.eulerAngles.y,
            targetAngle,
            ref rotationVelocity,
            rotationInertia);

        rb.MoveRotation(Quaternion.Euler(0, angle, 0));
    }

    private void ApplyVelocity(Vector3 moveDir)
    {
        Vector3 target = moveDir * moveSpeed;
        target.y = rb.linearVelocity.y;

        currentVelocity = Vector3.Lerp(rb.linearVelocity, target, movementInertia);
        rb.linearVelocity = currentVelocity;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        var managers = Core.Instance.Managers;
        var api = Core.Instance.ApiCommunicators;

        cameraManager = managers.CameraManager;
        movementCommunicator = api.PlayerMovement;

        joystick = FindObjectOfType<FixedJoystick>(); // UI-scoped, acceptable

        if (movementCommunicator == null)
            Debug.LogError("[LocalPlayerController] PlayerMovementCommunicator missing");
    }

    private async void FixedUpdate()
    {
        Vector3 moveDir = GetMoveDirection();
        ApplyMovement(moveDir);
        await TrySendNetworkUpdate();
    }

    private Vector3 GetMoveDirection()
    {
        Camera cam = cameraManager != null ? cameraManager.GetCamera() : Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 dir = Vector3.zero;
        var kb = Keyboard.current;

        if (kb != null)
        {
            if (kb.wKey.isPressed) dir += forward;
            if (kb.sKey.isPressed) dir -= forward;
            if (kb.aKey.isPressed) dir -= right;
            if (kb.dKey.isPressed) dir += right;
        }

        if (joystick != null && joystick.Direction.magnitude > 0.1f)
            dir = (forward * joystick.Vertical) + (right * joystick.Horizontal);

        return dir.sqrMagnitude > 1 ? dir.normalized : dir;
    }

    private void Start()
    {
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;
        lastSendTime = Time.time;
    }

    private async Task TrySendNetworkUpdate()
    {
        if (movementCommunicator == null) return;
        if (Time.time - lastSendTime < sendInterval) return;

        bool moved = Vector3.Distance(transform.position, lastPosition) > positionUpdateThreshold;
        bool rotated = Vector3.Distance(transform.rotation.eulerAngles, lastRotation) > rotationUpdateThreshold;

        if (!moved && !rotated) return;

        lastSendTime = Time.time;
        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;

        var t = new TransformationDC
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            positionZ = transform.position.z,
            rotationX = transform.rotation.eulerAngles.x,
            rotationY = transform.rotation.eulerAngles.y,
            rotationZ = transform.rotation.eulerAngles.z
        };

        await movementCommunicator.SendTransformation(t);
    }
}