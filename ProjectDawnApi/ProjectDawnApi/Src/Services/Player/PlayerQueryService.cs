using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi;

public class PlayerQueryService
{
    private readonly ProjectDawnDbContext db;

    public PlayerQueryService(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task<object?> GetPlayerAsync(int id)
    {
        return await db.Players
            .Where(p => p.Id == id)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.IsBanned,
                p.CreatedAtUtc
            })
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<object>> GetPlayersAsync()
    {
        return await db.Players
            .Select(p => new
            {
                p.Id,
                p.Name
            })
            .ToListAsync();
    }
}