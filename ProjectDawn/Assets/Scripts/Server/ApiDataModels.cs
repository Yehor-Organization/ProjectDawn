using System.Collections.Generic;

// These classes are designed to match the JSON structure returned by your FarmsController.

[System.Serializable]
public class FarmInfo
{
    public int id;
    public string name;
    public string ownerName;
}

[System.Serializable]
public class FarmState
{
    public int id;
    public string name;
    public string ownerName;
    public List<PlacedObject> placedObjects;
}

[System.Serializable]
public class PlacedObject
{
    public int id;
    public string type; // e.g., "Tree", "Fence", "Barn"
    public PositionDTO position;
    public float rotationY;
}

[System.Serializable]
public class PositionDTO
{
    public float x;
    public float y;
    public float z;
}
