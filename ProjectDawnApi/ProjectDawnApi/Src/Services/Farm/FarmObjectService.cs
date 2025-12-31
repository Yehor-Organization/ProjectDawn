using Microsoft.EntityFrameworkCore;

using ProjectDawnApi;

public class FarmObjectService
{
    private readonly ProjectDawnDbContext db;

    public FarmObjectService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task<ObjectDM> PlaceAsync(
        int playerId,
        int farmId,
        string type,
        TransformationDM transform)
    {
        bool isOwner = await db.Farms
            .AnyAsync(f =>
                f.Id == farmId &&
                f.Owners.Any(o => o.Id == playerId));

        if (!isOwner)
            throw new UnauthorizedAccessException("Not a farm owner");

        var obj = new ObjectDM
        {
            FarmId = farmId,
            Type = type,
            Transformation = transform
        };

        db.PlacedObjects.Add(obj);
        await db.SaveChangesAsync();

        return obj;
    }
}
