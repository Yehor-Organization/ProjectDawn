namespace ProjectDawnApi;
public class PlayerInventoryDBCommunicator
{
    public ProjectDawnDbContext Db { get; }

    public PlayerInventoryDBCommunicator(ProjectDawnDbContext db)
    {
        Db = db;
    }
}