using System.Collections.Generic;

[System.Serializable]
public class FarmStateDC
{
    public int id;
    public string name;
    public string ownerName;
    public List<PlacedObjectDC> placedObjects;
    public List<VisitorInfoDC> visitors;

}