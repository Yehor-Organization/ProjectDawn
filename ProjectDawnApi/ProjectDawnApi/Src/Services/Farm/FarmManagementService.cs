using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class FarmManagementService
{
    private readonly ProjectDawnDbContext db;

    public FarmManagementService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    /// <summary>
    /// Deletes a farm if the player is an owner.
    /// </summary>
    public async Task DeleteAsync(int farmId, int playerId)
    {
        var farm = await db.Farms
            .Include(f => f.Owners)
            .FirstOrDefaultAsync(f => f.Id == farmId);

        if (farm == null)
            throw new KeyNotFoundException("Farm not found.");

        bool isOwner = farm.Owners.Any(o => o.Id == playerId);
        if (!isOwner)
            throw new UnauthorizedAccessException("Not a farm owner.");

        db.Farms.Remove(farm);
        await db.SaveChangesAsync();
    }
}