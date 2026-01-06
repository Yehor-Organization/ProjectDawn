using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectDawnApi.Src.DataClasses.Visitor;

namespace ProjectDawnApi;

public class FarmSessionDBCommunicator
{
    private readonly ProjectDawnDbContext db;

    public FarmSessionDBCommunicator(ProjectDawnDbContext db)
    {
        this.db = db;
    }

    // =========================================================
    // ADD / REPLACE VISITOR (SINGLE SESSION GUARANTEE)
    // =========================================================
    public async Task AddVisitorAsync(int farmId, int playerId, string connId)
    {
        // Remove any existing session for this player
        var existing = await db.FarmVisitors
            .FirstOrDefaultAsync(v => v.PlayerId == playerId);

        if (existing != null)
            db.FarmVisitors.Remove(existing);

        db.FarmVisitors.Add(new VisitorDM
        {
            FarmId = farmId,
            PlayerId = playerId,
            ConnectionId = connId,
            Transformation = new TransformationDM(),
            JoinedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
    }

    // =========================================================
    // GET CURRENT FARM (AUTHORITATIVE)
    // =========================================================
    public async Task<int?> GetCurrentFarmForPlayerAsync(int playerId)
    {
        return await db.FarmVisitors
            .Where(v => v.PlayerId == playerId)
            .OrderByDescending(v => v.JoinedAtUtc)
            .Select(v => (int?)v.FarmId)
            .FirstOrDefaultAsync();
    }

    // =========================================================
    // GET OTHER PLAYERS IN FARM
    // =========================================================
    public Task<List<int>> GetOtherPlayersAsync(int farmId, int playerId)
        => db.FarmVisitors
            .Where(v => v.FarmId == farmId && v.PlayerId != playerId)
            .Select(v => v.PlayerId)
            .Distinct()
            .ToListAsync();

    // =========================================================
    // REMOVE BY CONNECTION (DISCONNECT)
    // =========================================================
    public async Task<VisitorDM?> RemoveByConnectionAsync(string connId)
    {
        var v = await db.FarmVisitors
            .FirstOrDefaultAsync(x => x.ConnectionId == connId);

        if (v == null)
            return null;

        db.FarmVisitors.Remove(v);
        await db.SaveChangesAsync();
        return v;
    }

    // =========================================================
    // REMOVE VISITOR EXPLICITLY
    // =========================================================
    public async Task RemoveVisitorAsync(int farmId, int playerId)
    {
        var v = await db.FarmVisitors
            .FirstOrDefaultAsync(x =>
                x.FarmId == farmId &&
                x.PlayerId == playerId);

        if (v == null)
            return;

        db.FarmVisitors.Remove(v);
        await db.SaveChangesAsync();
    }

    // =========================================================
    // FORCE LOGOUT FROM OTHER SESSION (MULTI-LOGIN PROTECTION)
    // =========================================================
    public async Task ReplaceExistingSessionAsync(
        Hub hub,
        int farmId,
        int playerId)
    {
        var existing = await db.FarmVisitors
            .FirstOrDefaultAsync(v => v.PlayerId == playerId);

        if (existing == null)
            return;

        await hub.Clients
            .Client(existing.ConnectionId)
            .SendAsync("Kicked", "Logged in elsewhere");

        await hub.Groups
            .RemoveFromGroupAsync(
                existing.ConnectionId,
                existing.FarmId.ToString());

        db.FarmVisitors.Remove(existing);
        await db.SaveChangesAsync();
    }
}