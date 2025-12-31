using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class FarmQueryService
{
    private readonly ProjectDawnDbContext db;

    public FarmQueryService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    /// <summary>
    /// Returns a single farm with owners, objects, visitors, and counts.
    /// </summary>
    public async Task<object?> GetFarmAsync(int farmId)
    {
        return await db.Farms
            .Include(f => f.Owners)
            .Include(f => f.Objects)
            .Include(f => f.Visitors)
                .ThenInclude(v => v.PlayerDataModel)
            .AsNoTracking()
            .Where(f => f.Id == farmId)
            .Select(f => new
            {
                f.Id,
                f.Name,

                Owners = f.Owners.Select(o => new
                {
                    o.Id,
                    o.Name
                }),

                VisitorCount = f.Visitors.Count,

                PlacedObjects = f.Objects.Select(o => new
                {
                    o.Id,
                    o.Type,
                    Transformation = new
                    {
                        o.Transformation.positionX,
                        o.Transformation.positionY,
                        o.Transformation.positionZ,
                        o.Transformation.rotationX,
                        o.Transformation.rotationY,
                        o.Transformation.rotationZ
                    }
                }),

                Visitors = f.Visitors.Select(v => new
                {
                    v.PlayerId,
                    PlayerName = v.PlayerDataModel != null
                        ? v.PlayerDataModel.Name
                        : "Unknown",
                    Transformation = new
                    {
                        v.Transformation.positionX,
                        v.Transformation.positionY,
                        v.Transformation.positionZ,
                        v.Transformation.rotationX,
                        v.Transformation.rotationY,
                        v.Transformation.rotationZ
                    }
                })
            })
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Returns all farms with owners and visitor counts.
    /// </summary>
    public async Task<IEnumerable<object>> GetFarmsAsync()
    {
        return await db.Farms
            .Include(f => f.Owners)
            .Include(f => f.Visitors)
                .ThenInclude(v => v.PlayerDataModel)
            .AsNoTracking()
            .Select(f => new
            {
                f.Id,
                f.Name,

                Owners = f.Owners.Select(o => new
                {
                    o.Id,
                    o.Name
                }),

                VisitorCount = f.Visitors.Count,

                Visitors = f.Visitors.Select(v => new
                {
                    v.PlayerId,
                    PlayerName = v.PlayerDataModel != null
                        ? v.PlayerDataModel.Name
                        : "Unknown"
                })
            })
            .ToListAsync();
    }
}