using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ProjectDawnApi;

public class PlayerAuthService
{
    private readonly IConfiguration config;
    private readonly ProjectDawnDbContext db;

    public PlayerAuthService(ProjectDawnDbContext db, IConfiguration config)
    {
        this.db = db;
        this.config = config;
    }

    // -----------------------
    // LOGIN
    // -----------------------
    public async Task<object> LoginAsync(PlayerDTO dto)
    {
        var player = await db.Players
            .Include(p => p.RefreshTokens)
            .FirstOrDefaultAsync(p => p.Username == dto.Username);

        if (player == null ||
            !PasswordHasher.Verify(dto.Password, player.PasswordHash))
            throw new UnauthorizedAccessException();

        if (player.IsBanned)
            throw new InvalidOperationException("BANNED");

        var accessToken = JwtTokenFactory.CreateToken(
            player.Id,
            player.Username,
            config);

        var refreshToken = new RefreshTokenDM
        {
            PlayerId = player.Id,
            Token = RefreshTokenFactory.Generate(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync();

        return new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            player = new
            {
                player.Id,
                player.Username
            }
        };
    }

    // -----------------------
    // REFRESH TOKEN
    // -----------------------
    public async Task<object> RefreshAsync(string refreshToken)
    {
        var stored = await db.RefreshTokens
            .Include(r => r.Player)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (stored == null ||
            stored.IsRevoked ||
            stored.ExpiresAtUtc < DateTime.UtcNow)
            throw new UnauthorizedAccessException();

        if (stored.Player.IsBanned)
            throw new InvalidOperationException("BANNED");

        stored.IsRevoked = true;

        var newRefresh = new RefreshTokenDM
        {
            PlayerId = stored.Player.Id,
            Token = RefreshTokenFactory.Generate(),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
        };

        db.RefreshTokens.Add(newRefresh);

        var newAccessToken = JwtTokenFactory.CreateToken(
            stored.Player.Id,
            stored.Player.Username,
            config);

        await db.SaveChangesAsync();

        return new
        {
            accessToken = newAccessToken,
            refreshToken = newRefresh.Token
        };
    }

    // -----------------------
    // REGISTER
    // -----------------------
    public async Task RegisterAsync(PlayerDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username))
            throw new ArgumentException("Name is required.");

        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Password is required.");

        if (dto.Password.Length < 6)
            throw new ArgumentException("Password must be at least 6 characters.");

        bool exists = await db.Players
            .AnyAsync(p => p.Username == dto.Username);

        if (exists)
            throw new InvalidOperationException("DUPLICATE");

        var player = new PlayerDM
        {
            Username = dto.Username,
            PasswordHash = PasswordHasher.Hash(dto.Password),
            IsBanned = false,
            CreatedAtUtc = DateTime.UtcNow,
            Inventory = new InventoryDM()
        };

        db.Players.Add(player);
        await db.SaveChangesAsync();
    }
}