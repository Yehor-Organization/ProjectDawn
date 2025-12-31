using Microsoft.AspNetCore.SignalR;

namespace ProjectDawnApi;
public class FarmObjectService
{
    private readonly ProjectDawnDbContext db;

    public FarmObjectService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task PlaceAsync(
        Hub hub,
        string farmIdStr,
        int playerId,
        string type,
        TransformationDM transform)
    {
        if (!int.TryParse(farmIdStr, out int farmId))
            return;

        var id = Guid.NewGuid();

        await hub.Clients.Group(farmIdStr)
            .SendAsync("ObjectPlaced", id, type, transform);

        db.PlacedObjects.Add(new PlacedObjectDM
        {
            Id = id,
            FarmId = farmId,
            Type = type,
            Transformation = transform
        });

        await db.SaveChangesAsync();
    }
}

