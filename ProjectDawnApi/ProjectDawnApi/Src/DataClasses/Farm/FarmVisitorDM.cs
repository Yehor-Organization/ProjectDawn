
namespace ProjectDawnApi;

public class FarmVisitorDM
{
    public int FarmId { get; set; }
    public FarmDM Farm { get; set; }

    public int PlayerId { get; set; }
    public PlayerDM PlayerDataModel { get; set; }

    // Nullable because visitors may exist before joining SignalR
    public string? ConnectionId { get; set; }

    public TransformationDM Transformation { get; set; } 
}
