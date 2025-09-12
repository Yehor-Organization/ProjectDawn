using UnityEngine;

/// <summary>
/// A simple script for remote player prefabs. It smoothly moves the character
/// towards a target position and rotation received from the server.
/// </summary>
public class RemotePlayerController : MonoBehaviour
{
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private float moveLerpSpeed;
    private float rotateLerpSpeed;
    public void Initialize(float moveSpeed, float rotateSpeed)
    {
        moveLerpSpeed = moveSpeed;
        rotateLerpSpeed = rotateSpeed;
    }
    void Awake()
    {
        // Initialize target position and rotation to the starting values to avoid weird jumps
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    /// <summary>
    /// Called by the PlayerManager to set the new position for this character to move to.
    /// </summary>
    public void SetTargetTransformation(TransformationDataModel newTransformation)
    {
        targetPosition = new Vector3(
            newTransformation.positionX,
            newTransformation.positionY,
            newTransformation.positionZ
        );

        targetRotation = Quaternion.Euler(
            newTransformation.rotationX,
            newTransformation.rotationY,
            newTransformation.rotationZ
        );
    }


    void Update()
    {
        if (transform.position != targetPosition)
        {
            Debug.Log($"[DEBUG][RemotePlayerController] Moving from {transform.position} → {targetPosition}");
        }
        if (transform.rotation != targetRotation)
        {
            Debug.Log($"[DEBUG][RemotePlayerController] Rotating from {transform.rotation.eulerAngles} → {targetRotation.eulerAngles}");
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveLerpSpeed);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotateLerpSpeed);
    }
}