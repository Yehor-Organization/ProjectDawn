using System.Collections.Generic;

[System.Serializable]
public class FarmStateDto
{
    public int id;
    public string name;
    public string ownerName;
    public List<PlacedObject> placedObjects;
    public List<VisitorInfoDto> visitors;

}