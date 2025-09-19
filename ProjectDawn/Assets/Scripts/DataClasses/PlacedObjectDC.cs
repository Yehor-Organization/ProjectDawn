
[System.Serializable]
public class PlacedObjectDC
{
    public string objectId; // Guid from server (as string in JSON)
    public string type;
    public TransformationDC transformation;
}