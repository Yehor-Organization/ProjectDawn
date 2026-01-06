using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ProjectDawnApi.Src.Controllers.Players;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly PlayerInventoryService inventoryService;

    public InventoryController(PlayerInventoryService inventoryService)
    {
        this.inventoryService = inventoryService;
    }

    private int PlayerId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ---------------------------------
    // ADD ITEM
    // ---------------------------------
    [HttpPost("[action]")]
    public async Task<IActionResult> AddItem([FromBody] AddItemDTO dto)
    {
        try
        {
            await inventoryService.AddItemAsync(
                PlayerId,
                dto.ItemType,
                dto.Quantity);

            return Ok(new
            {
                itemType = dto.ItemType,
                quantityAdded = dto.Quantity
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Inventory not found.");
        }
    }

    // ---------------------------------
    // GET INVENTORY
    // ---------------------------------
    [HttpGet("[action]")]
    public async Task<IActionResult> GetInventory()
    {
        try
        {
            var inventory = await inventoryService
                .GetInventoryAsync(PlayerId);

            return Ok(inventory);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("Inventory not found.");
        }
    }
}