using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 15f;
    public float rotateSpeed = 200f; // keyboard rotation speed (degrees/sec)
    private Camera playerCamera;
    private Vector3 cameraOffset = new Vector3(0, 2, -5);

    private FixedJoystick joystick;

    void Start()
    {
        if (IsOwner)
        {
            // Camera
            playerCamera = Camera.main;
            if (playerCamera != null)
            {
                // Compute initial offset from player to camera
                cameraOffset = playerCamera.transform.position - transform.position;
                playerCamera.gameObject.SetActive(true);
            }

            // Find joystick in the scene
            joystick = FindObjectOfType<FixedJoystick>();
            if (joystick == null)
                Debug.LogWarning("No joystick found in scene!");
        }
    }


    void LateUpdate()
    {
        if (!IsOwner || playerCamera == null) return;

        playerCamera.transform.position = transform.position + cameraOffset;
        playerCamera.transform.LookAt(transform.position + Vector3.up);

    }

    void Update()
    {
        if (!IsOwner) return;

        float h = 0f;
        float v = 0f;

        // --- Keyboard input ---
        if (Keyboard.current.wKey.isPressed) v += 1f;
        if (Keyboard.current.sKey.isPressed) v -= 1f;
        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;

        // --- Joystick input ---
        Vector3 joystickDir = Vector3.zero;
        if (joystick != null)
        {
            joystickDir = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
            if (joystickDir.magnitude > 0.1f)
            {
                // Instant rotation toward joystick direction
                transform.forward = joystickDir.normalized;
                v = joystickDir.magnitude; // move forward according to joystick
                h = 0f; // ignore keyboard rotation if joystick used
            }
        }

        // Move forward/backward
        transform.position += transform.forward * v * moveSpeed * Time.deltaTime;

        // Rotate left/right with keyboard only
        if (h != 0f && joystickDir.magnitude <= 0.1f)
        {
            transform.Rotate(Vector3.up * h * rotateSpeed * Time.deltaTime);
        }

        // Up/down
        if (Keyboard.current.spaceKey.isPressed)
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        if (Keyboard.current.leftCtrlKey.isPressed)
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }
}
