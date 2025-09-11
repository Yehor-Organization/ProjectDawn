using UnityEngine;

/// <summary>
/// A simple script for remote player prefabs. It smoothly moves the character
/// towards a target position received from the server.
/// </summary>
public class RemotePlayerController : MonoBehaviour
{
    private Vector3 targetPosition;
    private float lerpSpeed = 10f; // How quickly the player model snaps to the target position

    void Awake()
    {
        // Initialize target position to the starting position to avoid weird jumps
        targetPosition = transform.position;
    }

    /// <summary>
    /// Called by the PlayerManager to set the new position for this character to move to.
    /// </summary>
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }

    void Update()
    {
        // Smoothly interpolate the character's position towards the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * lerpSpeed);
    }
}
