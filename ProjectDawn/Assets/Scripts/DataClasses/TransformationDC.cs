using UnityEngine;

[System.Serializable]
public class TransformationDC
{
    public float positionX { get; set; }
    public float positionY { get; set; }
    public float positionZ { get; set; }

    public float rotationX { get; set; }
    public float rotationY { get; set; }
    public float rotationZ { get; set; }

    // ✅ CRITICAL
    public float serverTime { get; set; }

    // Core factory
    public static TransformationDC FromVectors(
        Vector3? position = null,
        Vector3? rotation = null,
        float serverTime = 0f)
    {
        return new TransformationDC
        {
            positionX = position?.x ?? 0f,
            positionY = position?.y ?? 0f,
            positionZ = position?.z ?? 0f,
            rotationX = rotation?.x ?? 0f,
            rotationY = rotation?.y ?? 0f,
            rotationZ = rotation?.z ?? 0f,
            serverTime = serverTime
        };
    }

    public Vector3 ToPosition()
        => new(positionX, positionY, positionZ);

    public Quaternion ToRotation()
        => Quaternion.Euler(rotationX, rotationY, rotationZ);
}