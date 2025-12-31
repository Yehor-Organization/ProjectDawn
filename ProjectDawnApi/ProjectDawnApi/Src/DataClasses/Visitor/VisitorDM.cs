namespace ProjectDawnApi.Src.DataClasses.Visitor;

public class VisitorDM
{
    // Nullable because visitors may exist before joining SignalR
    public string? ConnectionId { get; set; }

    public FarmDM Farm { get; set; }
    public int FarmId { get; set; }
    public PlayerDM PlayerDataModel { get; set; }
    public int PlayerId { get; set; }
    public TransformationDM Transformation { get; set; }
}