using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProjectDawnApi.Src.Controllers.Players;
[Route("api/[controller]")]
[ApiController]
public class InventoryController : ControllerBase
{
    private readonly ProjectDawnDbContext context;
    private readonly PlayerInventoryService inventoryService;

    public InventoryController(
        ProjectDawnDbContext context,
        PlayerInventoryService inventoryService)
    {
        this.context = context;
        this.inventoryService = inventoryService;
    }

    [HttpGet("{playerId}")]
    public async Task<IActionResult> GetInventory(int playerId)
    {
        var inventory = await context.Inventories
            .Include(i => i.Items)
            .Where(i => i.PlayerId == playerId)
            .Select(i => new
            {
                i.PlayerId,
                Items = i.Items.Select(item => new
                {
                    item.ItemType,
                    item.Quantity
                })
            })
            .FirstOrDefaultAsync();

        if (inventory == null)
            return NotFound();

        return Ok(inventory);
    }

    [HttpPost("{playerId}/Add")]
    public async Task<IActionResult> AddItem(
    int playerId,
    [FromBody] AddItemDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemType))
            return BadRequest("ItemType is required.");

        if (dto.Quantity <= 0)
            return BadRequest("Quantity must be positive.");

        var inventory = await context.Inventories
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.PlayerId == playerId);

        if (inventory == null)
            return NotFound("Inventory not found.");

        // Merge stacks
        var existingItem = inventory.Items
            .FirstOrDefault(i => i.ItemType == dto.ItemType);

        if (existingItem != null)
        {
            existingItem.Quantity += dto.Quantity;
        }
        else
        {
            inventory.Items.Add(new InventoryItemDM
            {
                ItemType = dto.ItemType,
                Quantity = dto.Quantity
            });
        }

        // ✅ Commit authoritative state
        await context.SaveChangesAsync();

        // 🔔 REAL-TIME UPDATE VIA PLAYER HUB
        await inventoryService.NotifyInventoryUpdatedAsync(
            playerId,
            new
            {
                itemType = dto.ItemType,
                delta = dto.Quantity
            });

        return Ok(new
        {
            playerId,
            itemType = dto.ItemType,
            quantityAdded = dto.Quantity
        });
    }
}