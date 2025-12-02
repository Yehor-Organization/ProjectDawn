using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectDawnApi
{
    [Route("api/players/{playerId}/inventory")]
    [ApiController]
    public class InventoryController : ControllerBase
    {
        private readonly ProjectDawnDbContext _context;

        public InventoryController(ProjectDawnDbContext context)
        {
            _context = context;
        }

        // GET: api/players/{playerId}/inventory
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetInventory(int playerId)
        {
            var inventory = await _context.InventoryItems
                .Where(i => i.PlayerId == playerId)
                .Select(i => new
                {
                    i.ItemName,
                    i.Quantity
                })
                .ToListAsync();

            return Ok(inventory);
        }

        public class AddItemRequest
        {
            public string ItemName { get; set; } = string.Empty;
            public int Amount { get; set; } = 1;
        }

        // POST: api/players/{playerId}/inventory/add
        [HttpPost("add")]
        public async Task<IActionResult> AddItem(int playerId, [FromBody] AddItemRequest request)
        {
            var player = await _context.Players.FindAsync(playerId);
            if (player == null)
            {
                return NotFound("Player not found");
            }

            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == playerId && i.ItemName == request.ItemName);

            if (inventoryItem != null)
            {
                inventoryItem.Quantity += request.Amount;
            }
            else
            {
                inventoryItem = new InventoryItemDataModel
                {
                    PlayerId = playerId,
                    ItemName = request.ItemName,
                    Quantity = request.Amount
                };
                _context.InventoryItems.Add(inventoryItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { inventoryItem.ItemName, inventoryItem.Quantity });
        }

        // POST: api/players/{playerId}/inventory/remove
        [HttpPost("remove")]
        public async Task<IActionResult> RemoveItem(int playerId, [FromBody] AddItemRequest request)
        {
            var inventoryItem = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.PlayerId == playerId && i.ItemName == request.ItemName);

            if (inventoryItem == null)
            {
                return NotFound("Item not found in inventory");
            }

            inventoryItem.Quantity -= request.Amount;

            if (inventoryItem.Quantity <= 0)
            {
                _context.InventoryItems.Remove(inventoryItem);
            }

            await _context.SaveChangesAsync();
            return Ok(new { inventoryItem.ItemName, inventoryItem.Quantity });
        }
    }
}
