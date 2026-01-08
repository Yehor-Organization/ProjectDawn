using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ProjectDawnApi.Src.Controllers.Players;

[ApiController]
[Route("[controller]")]
public class PlayersController : ControllerBase
{
    private readonly PlayerAuthService authService;
    private readonly PlayerQueryService queryService;

    public PlayersController(
        PlayerQueryService queryService,
        PlayerAuthService authService)
    {
        this.queryService = queryService;
        this.authService = authService;
    }

    // -----------------------
    // GET PLAYER
    // -----------------------
    [Authorize]
    [HttpGet("[action]/{id:int}")]
    public async Task<IActionResult> GetPlayer(int id)
    {
        var player = await queryService.GetPlayerAsync(id);
        return player == null ? NotFound() : Ok(player);
    }

    // -----------------------
    // GET PLAYERS
    // -----------------------
    [Authorize]
    [HttpGet("[action]")]
    public async Task<IActionResult> GetPlayers()
    {
        var players = await queryService.GetPlayersAsync();
        return Ok(players);
    }

    // -----------------------
    // LOGIN
    // -----------------------
    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] PlayerDTO dto)
    {
        try
        {
            return Ok(await authService.LoginAsync(dto));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "BANNED")
        {
            return Forbid();
        }
    }

    // -----------------------
    // REFRESH TOKEN
    // -----------------------
    [HttpPost("[action]")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenDTO dto)
    {
        try
        {
            return Ok(await authService.RefreshAsync(dto.RefreshToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (InvalidOperationException ex) when (ex.Message == "BANNED")
        {
            return Forbid();
        }
    }

    // -----------------------
    // REGISTER
    // -----------------------
    [HttpPost("[action]")]
    public async Task<IActionResult> Register([FromBody] PlayerDTO dto)
    {
        try
        {
            await authService.RegisterAsync(dto);
            return Ok(new { message = "Player registered successfully" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message == "DUPLICATE")
        {
            return Conflict("Player name already exists.");
        }
    }
}