using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles input, movement, and camera for the player this client controls.
/// This script should be placed on your "Local Player" prefab.
/// </summary>
public class LocalPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 15f;
    public float rotateSpeed = 200f;

    [Header("Camera")]
    public Vector3 cameraOffset = new Vector3(0, 10, -8);
    private Camera playerCamera;

    // --- Joystick ---
    private FixedJoystick joystick;

    // --- Private References ---
    private ProjectDawnApi networkClient;
    private Vector3 lastPosition;
    private float positionUpdateThreshold = 0.05f; // How far the player must move to send an update

    void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera != null)
        {
            playerCamera.transform.position = transform.position + cameraOffset;
            playerCamera.transform.LookAt(transform.position);
        }

        // Find joystick in the scene
        joystick = FindObjectOfType<FixedJoystick>();
        if (joystick == null)
            Debug.LogWarning("No joystick found in scene!");

        // Find the network client in the scene to send position updates
        networkClient = FindObjectOfType<ProjectDawnApi>();
        if (networkClient == null)
        {
            Debug.LogError("Could not find ProjectDawnApi script in the scene!");
        }

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
                // Rotate instantly toward joystick direction
                transform.forward = joystickDir.normalized;
                v = joystickDir.magnitude; // move forward according to joystick strength
                h = 0f; // disable keyboard rotation while joystick is active
            }
        }

        // --- Apply Movement ---
        transform.position += transform.forward * v * moveSpeed * Time.deltaTime;

        if (h != 0f && joystickDir.magnitude <= 0.1f)
            transform.Rotate(Vector3.up * h * rotateSpeed * Time.deltaTime);

        // --- Send Position Update to Server ---
        if (Vector3.Distance(transform.position, lastPosition) > positionUpdateThreshold)
        {
            if (networkClient != null)
            {
                lastPosition = transform.position;
                networkClient.SendPositionUpdate(lastPosition);
            }
        }
    }

    void LateUpdate()
    {
        // Keep the camera locked to the player
        if (playerCamera != null)
        {
            playerCamera.transform.position = transform.position + cameraOffset;
            playerCamera.transform.LookAt(transform.position);
        }
    }
}
