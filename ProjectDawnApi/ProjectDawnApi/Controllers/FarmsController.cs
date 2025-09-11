using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectDawnApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmsController : ControllerBase
    {
        private readonly ProjectDawnDbContext _context;

        public FarmsController(ProjectDawnDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFarms()
        {
            var farms = await _context.Farms
                .Include(f => f.Owner)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    OwnerName = f.Owner != null ? f.Owner.Name : "N/A"
                })
                .ToListAsync();

            return Ok(farms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFarm(int id)
        {
            var farm = await _context.Farms
                .Include(f => f.Owner)
                .Include(f => f.PlacedObjects)
                .Include(f => f.Visitors)
                    .ThenInclude(v => v.PlayerDataModel)
                .Where(f => f.Id == id)
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    OwnerName = f.Owner != null ? f.Owner.Name : "N/A",
                    PlacedObjects = f.PlacedObjects.Select(po => new
                    {
                        po.Id,
                        po.Type,
                        Position = new { X = po.PositionX, Y = po.PositionY, Z = po.PositionZ },
                        po.RotationY
                    }),
                    Visitors = f.Visitors.Select(v => new
                    {
                        v.PlayerId,
                        PlayerName = v.PlayerDataModel != null ? v.PlayerDataModel.Name : "Unknown",
                        Position = new { X = v.LastPositionX, Y = v.LastPositionY, Z = v.LastPositionZ }
                    })
                })
                .FirstOrDefaultAsync();

            if (farm == null) return NotFound();

            return Ok(farm);
        }
    }
}
