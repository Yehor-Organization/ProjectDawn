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
    public string type;
    public TransformationDataModel transformation;
}

[System.Serializable]
public class TransformationDataModel
{
    public float positionX { get; set; }
    public float positionY { get; set; }
    public float positionZ { get; set; }
    public float rotationX { get; set; }
    public float rotationY { get; set; }
    public float rotationZ { get; set; }
}
