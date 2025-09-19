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

    // Core factory: takes optional position and rotation
    public static TransformationDC FromVectors(Vector3? position = null, Vector3? rotation = null)
    {
        var dc = new TransformationDC();

        if (position.HasValue)
        {
            dc.positionX = position.Value.x;
            dc.positionY = position.Value.y;
            dc.positionZ = position.Value.z;
        }

        if (rotation.HasValue)
        {
            dc.rotationX = rotation.Value.x;
            dc.rotationY = rotation.Value.y;
            dc.rotationZ = rotation.Value.z;
        }

        return dc;
    }

    // Convenience wrappers
    public static TransformationDC FromPosition(Vector3 position) => FromVectors(position, null);
    public static TransformationDC FromRotation(Vector3 rotation) => FromVectors(null, rotation);

    // Back to Unity types
    public Vector3 ToPosition() => new Vector3(positionX, positionY, positionZ);
    public Quaternion ToRotation() => Quaternion.Euler(rotationX, rotationY, rotationZ);
}