using ProjectDawnApi;

public class VisitorDM
{
    public int Id { get; set; }   // ✅ PRIMARY KEY

    public int FarmId { get; set; }
    public FarmDM Farm { get; set; } = null!;

    public int PlayerId { get; set; }
    public PlayerDM PlayerDataModel { get; set; } = null!;

    public string ConnectionId { get; set; } = string.Empty;

    public TransformationDM Transformation { get; set; } = new();

    public DateTime JoinedAtUtc { get; set; }
}