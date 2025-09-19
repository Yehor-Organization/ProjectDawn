using System.Collections.Generic;

[System.Serializable]
public class FarmStateDC
{
    public string id; // farm id
    public string name;
    public string ownerName;
    public List<PlacedObjectDC> placedObjects;
}