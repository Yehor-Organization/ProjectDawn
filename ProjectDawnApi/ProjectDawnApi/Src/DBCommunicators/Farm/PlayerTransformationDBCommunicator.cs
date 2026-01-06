using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class PlayerTransformationDBCommunicator
{
    private readonly ProjectDawnDbContext db;

    public PlayerTransformationDBCommunicator(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task UpdateVisitorTransformationAsync(
        int farmId,
        int playerId,
        TransformationDM transform)
    {
        var visitor = await db.FarmVisitors
            .FirstOrDefaultAsync(v =>
                v.FarmId == farmId &&
                v.PlayerId == playerId);

        if (visitor == null)
            return;

        visitor.Transformation = transform;
        await db.SaveChangesAsync();
    }
}