namespace ProjectDawnApi;

public class FarmInfoDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int VisitorCount { get; set; }
}