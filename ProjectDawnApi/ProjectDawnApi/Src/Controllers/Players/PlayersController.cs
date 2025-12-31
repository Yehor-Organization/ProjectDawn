using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi.Src.Controllers.Players
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly ProjectDawnDbContext context;

        public PlayersController(ProjectDawnDbContext context)
        {
            this.context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> RegisterPlayer([FromBody] PlayerDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest("Name is required.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Password is required.");

            if (dto.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            // Prevent duplicate player names
            var exists = await context.Players
                .AnyAsync(p => p.Name == dto.Name);

            if (exists)
                return Conflict("Player name already exists.");

            // 🔐 Hash password
            var passwordHash = PasswordHasher.Hash(dto.Password);

            // 🧍 Create player
            var player = new PlayerDM
            {
                Name = dto.Name,
                PasswordHash = passwordHash,
                IsBanned = false,
                CreatedAtUtc = DateTime.UtcNow
            };

            // 🎒 Create inventory (1:1)
            var inventory = new InventoryDM
            {
                Player = player
            };

            player.Inventory = inventory;

            context.Players.Add(player);
            await context.SaveChangesAsync();

            // ✅ Return ONLY player info
            return CreatedAtAction(
                nameof(GetPlayer),
                new { id = player.Id },
                new
                {
                    player.Id,
                    player.Name,
                    player.CreatedAtUtc
                }
            );
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] PlayerDTO dto,
            [FromServices] IConfiguration config)
        {
            var player = await context.Players
                .Include(p => p.RefreshTokens)
                .FirstOrDefaultAsync(p => p.Name == dto.Name);

            if (player == null ||
                !PasswordHasher.Verify(dto.Password, player.PasswordHash))
                return Unauthorized();

            if (player.IsBanned)
                return Forbid();

            // 🔐 Create JWT
            var accessToken = JwtTokenFactory.CreateToken(
                player.Id,
                player.Name,
                config);

            // 🔁 Create refresh token
            var refreshToken = new RefreshTokenDM
            {
                PlayerId = player.Id,
                Token = RefreshTokenFactory.Generate(),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
            };

            context.RefreshTokens.Add(refreshToken);
            await context.SaveChangesAsync();

            return Ok(new
            {
                accessToken,
                refreshToken = refreshToken.Token,
                player = new
                {
                    player.Id,
                    player.Name
                }
            });
        }

        [HttpPost("Refresh")]
        public async Task<IActionResult> Refresh(
    [FromBody] RefreshTokenDTO dto,
    [FromServices] IConfiguration config)
        {
            var storedToken = await context.RefreshTokens
                .Include(r => r.Player)
                .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken);

            if (storedToken == null ||
                storedToken.IsRevoked ||
                storedToken.ExpiresAtUtc < DateTime.UtcNow)
                return Unauthorized();

            var player = storedToken.Player;

            if (player.IsBanned)
                return Forbid();

            // Rotate token (VERY IMPORTANT)
            storedToken.IsRevoked = true;

            var newRefreshToken = new RefreshTokenDM
            {
                PlayerId = player.Id,
                Token = RefreshTokenFactory.Generate(),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(14)
            };

            context.RefreshTokens.Add(newRefreshToken);

            var newAccessToken = JwtTokenFactory.CreateToken(
                player.Id,
                player.Name,
                config);

            await context.SaveChangesAsync();

            return Ok(new
            {
                accessToken = newAccessToken,
                refreshToken = newRefreshToken.Token
            });
        }


        [HttpGet]
        public async Task<IActionResult> GetPlayers()
        {
            var players = await context.Players
                .Select(p => new
                {
                    p.Id,
                    p.Name
                })
                .ToListAsync();

            return Ok(players);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlayer(int id)
        {
            var player = await context.Players
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.IsBanned,
                    p.CreatedAtUtc
                })
                .FirstOrDefaultAsync();

            if (player == null)
                return NotFound();

            return Ok(player);
        }
    }

}
