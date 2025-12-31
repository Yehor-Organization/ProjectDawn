using Microsoft.EntityFrameworkCore;
using ProjectDawnApi;

public class FarmCreationService
{
    private readonly ProjectDawnDbContext db;

    public FarmCreationService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task<FarmDM> CreateAsync(int playerId, string name)
    {
        bool exists = await db.Farms
            .AnyAsync(f =>
                f.Name == name &&
                f.Owners.Any(o => o.Id == playerId));

        if (exists)
            throw new InvalidOperationException("Duplicate farm name");

        var player = await db.Players.FindAsync(playerId)
            ?? throw new UnauthorizedAccessException();

        var farm = new FarmDM { Name = name };
        farm.Owners.Add(player);

        db.Farms.Add(farm);
        await db.SaveChangesAsync();

        return farm;
    }
}