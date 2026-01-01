using System.Collections.Generic;

[System.Serializable]
public class FarmStateDM
{
    public string id;                // farm id
    public string name;
    public string ownerName;

    // Objects currently placed in the farm
    public List<PlacedObjectDC> placedObjects;

    // Players currently in the farm
    public List<VisitorInfoDC> visitors;
}