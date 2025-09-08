using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Camera playerCamera;
    private Vector3 cameraOffset = new Vector3(0, 2, -5);

    private FixedJoystick joystick;

    void Start()
    {
        if (IsOwner)
        {
            playerCamera = Camera.main;
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(true);

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

        // Keyboard input
        if (Keyboard.current.wKey.isPressed) v += 1f;
        if (Keyboard.current.sKey.isPressed) v -= 1f;
        if (Keyboard.current.aKey.isPressed) h -= 1f;
        if (Keyboard.current.dKey.isPressed) h += 1f;

        // Joystick input
        if (joystick != null)
        {
            h += joystick.Horizontal;
            v += joystick.Vertical;
        }

        Vector3 moveDirection = new Vector3(h, 0, v);

        if (moveDirection.magnitude > 0.1f)
        {
            // Rotate immediately toward joystick direction
            transform.forward = moveDirection.normalized;

            // Move in that direction
            transform.position += moveDirection.normalized * moveSpeed * Time.deltaTime;
        }

        // Up/down with keyboard
        if (Keyboard.current.spaceKey.isPressed)
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        if (Keyboard.current.leftCtrlKey.isPressed)
            transform.position += Vector3.down * moveSpeed * Time.deltaTime;
    }
}
