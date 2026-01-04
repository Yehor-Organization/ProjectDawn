using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class FarmQueryService
{
    private readonly ProjectDawnDbContext db;

    public FarmQueryService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    // =========================================================
    // ✔ NEW: typed farm info for SignalR / lobby UI
    // =========================================================

    public async Task<FarmInfoDTO?> GetFarmInfoAsync(string farmId)
    {
        if (!int.TryParse(farmId, out var id))
            return null;

        return await db.Farms
            .AsNoTracking()
            .Include(f => f.Owners)
            .Include(f => f.Visitors)
            .Where(f => f.Id == id)
            .Select(f => new FarmInfoDTO
            {
                Id = f.Id,
                Name = f.Name,
                OwnerName = f.Owners
                    .Select(o => o.Username)
                    .FirstOrDefault() ?? "Unknown",
                VisitorCount = f.Visitors.Count
            })
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // EXISTING: full farm (scene load)
    // =========================================================

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
                    o.Username
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
                        ? v.PlayerDataModel.Username
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

    // =========================================================
    // EXISTING: farm list (REST)
    // =========================================================

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
                    o.Username
                }),

                VisitorCount = f.Visitors.Count,

                Visitors = f.Visitors.Select(v => new
                {
                    v.PlayerId,
                    PlayerName = v.PlayerDataModel != null
                        ? v.PlayerDataModel.Username
                        : "Unknown"
                })
            })
            .ToListAsync();
    }
}