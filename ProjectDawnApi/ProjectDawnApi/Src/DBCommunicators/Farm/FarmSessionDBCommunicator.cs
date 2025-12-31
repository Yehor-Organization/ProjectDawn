using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectDawnApi.Src.DataClasses.Visitor;

namespace ProjectDawnApi.Src.DBCommunicators.Farm;

public class FarmSessionDBCommunicator
{
    private readonly ProjectDawnDbContext db;

    public FarmSessionDBCommunicator(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    public async Task AddVisitorAsync(int farmId, int playerId, string connId)
    {
        db.FarmVisitors.Add(new VisitorDM
        {
            FarmId = farmId,
            PlayerId = playerId,
            ConnectionId = connId,
            Transformation = new TransformationDM()
        });
        await db.SaveChangesAsync();
    }

    public async Task<int?> GetCurrentFarmForPlayerAsync(int playerId)
    {
        var visitor = await db.FarmVisitors
            .Where(v => v.PlayerId == playerId)
            .Select(v => (int?)v.FarmId)
            .FirstOrDefaultAsync();

        return visitor;
    }

    public Task<List<int>> GetOtherPlayersAsync(int farmId, int playerId)
        => db.FarmVisitors
            .Where(v => v.FarmId == farmId && v.PlayerId != playerId)
            .Select(v => v.PlayerId)
            .ToListAsync();

    public async Task<VisitorDM?> RemoveByConnectionAsync(string connId)
    {
        var v = await db.FarmVisitors.FirstOrDefaultAsync(x => x.ConnectionId == connId);
        if (v == null) return null;

        db.FarmVisitors.Remove(v);
        await db.SaveChangesAsync();
        return v;
    }

    public async Task RemoveVisitorAsync(int farmId, int playerId)
    {
        var v = await db.FarmVisitors.FirstOrDefaultAsync(x => x.FarmId == farmId && x.PlayerId == playerId);
        if (v != null)
        {
            db.FarmVisitors.Remove(v);
            await db.SaveChangesAsync();
        }
    }

    public async Task ReplaceExistingSessionAsync(Hub hub, int farmId, int playerId)
    {
        var existing = await db.FarmVisitors.FirstOrDefaultAsync(v => v.PlayerId == playerId);
        if (existing == null) return;

        await hub.Clients.Client(existing.ConnectionId)
            .SendAsync("Kicked", "Logged in elsewhere");

        await hub.Groups.RemoveFromGroupAsync(existing.ConnectionId, existing.FarmId.ToString());
        db.FarmVisitors.Remove(existing);
        await db.SaveChangesAsync();
    }
}