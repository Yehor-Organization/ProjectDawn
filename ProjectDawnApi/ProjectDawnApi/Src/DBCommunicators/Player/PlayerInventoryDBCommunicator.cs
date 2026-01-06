using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class PlayerInventoryDBCommunicator
{
    private readonly ProjectDawnDbContext db;

    public PlayerInventoryDBCommunicator(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public Task<InventoryDM?> GetInventoryWithItemsAsync(int playerId)
        => db.Inventories
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.PlayerId == playerId);

    public Task SaveChangesAsync()
        => db.SaveChangesAsync();
}