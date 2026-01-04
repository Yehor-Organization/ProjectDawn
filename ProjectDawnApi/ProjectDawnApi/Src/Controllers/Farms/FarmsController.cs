using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProjectDawnApi.Src.Services.Farm;
using System.Security.Claims;

namespace ProjectDawnApi.Src.Controllers.Farms;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FarmsController : ControllerBase
{
    private readonly FarmCreationService farmCreationService;
    private readonly FarmManagementService farmManagementService;
    private readonly FarmObjectService farmObjectService;
    private readonly FarmQueryService farmQueryService;
    private readonly FarmSessionService farmSessionService;
    private readonly IHubContext<FarmHub> hubContext;

    public FarmsController(
        FarmCreationService farmCreationService,
        FarmManagementService farmManagementService,
        FarmQueryService farmQueryService,
        FarmObjectService farmObjectService,
        FarmSessionService farmSessionService,
        IHubContext<FarmHub> hubContext)
    {
        this.farmCreationService = farmCreationService;
        this.farmManagementService = farmManagementService;
        this.farmQueryService = farmQueryService;
        this.farmObjectService = farmObjectService;
        this.farmSessionService = farmSessionService;
        this.hubContext = hubContext;
    }

    private int PlayerId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("[action]")]
    public async Task<IActionResult> CreateFarm(
        [FromBody] CreateFarmRequestDTO request)
    {
        try
        {
            var farm = await farmCreationService.CreateAsync(
                PlayerId,
                request.Name);

            return CreatedAtAction(
                nameof(GetFarm),
                new { id = farm.Id },
                new
                {
                    farm.Id,
                    farm.Name,
                    Owners = farm.Owners.Select(o => new
                    {
                        o.Id,
                        o.Username
                    })
                });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpDelete("[action]/{id:int}")]
    public async Task<IActionResult> DeleteFarm(int id)
    {
        try
        {
            await farmManagementService.DeleteAsync(id, PlayerId);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpGet("[action]/{id:int}")]
    public async Task<IActionResult> GetFarm(int id)
    {
        var farm = await farmQueryService.GetFarmAsync(id);
        return farm == null ? NotFound() : Ok(farm);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetFarms()
    {
        var farms = await farmQueryService.GetFarmsAsync();
        return Ok(farms);
    }

    [HttpGet("[action]")]
    public async Task<IActionResult> GetObjects()
    {
        int? farmId =
            await farmSessionService.GetCurrentFarmForPlayerAsync(PlayerId);

        if (farmId == null)
            return BadRequest("Player is not in a farm.");

        try
        {
            var objects = await farmObjectService.GetAllAsync(
                PlayerId,
                farmId.Value);

            return Ok(objects);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> PlaceObject(
        [FromBody] PlaceObjectDTO dto)
    {
        int? farmId =
            await farmSessionService.GetCurrentFarmForPlayerAsync(PlayerId);

        if (farmId == null)
            return BadRequest("Player is not in a farm.");

        try
        {
            var obj = await farmObjectService.PlaceAsync(
                PlayerId,
                farmId.Value,
                dto.Type,
                dto.Transformation);

            await hubContext.Clients
                .Group(farmId.Value.ToString())
                .SendAsync("ObjectPlaced", obj);

            return Ok(obj);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}