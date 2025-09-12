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
                .AsNoTracking() // 👈 prevents tracking issues
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    OwnerName = f.Owner != null ? f.Owner.Name : "N/A",
                    PlacedObjects = f.PlacedObjects.Select(po => new
                    {
                        po.Id,
                        po.Type,
                        Transformation = new // 👈 project fields manually
                        {
                            po.Transformation.positionX,
                            po.Transformation.positionY,
                            po.Transformation.positionZ,
                            po.Transformation.rotationX,
                            po.Transformation.rotationY,
                            po.Transformation.rotationZ
                        }
                    }),
                    Visitors = f.Visitors.Select(v => new
                    {
                        v.PlayerId,
                        PlayerName = v.PlayerDataModel != null ? v.PlayerDataModel.Name : "Unknown",
                        Transformation = new // 👈 project fields manually
                        {
                            v.Transformation.positionX,
                            v.Transformation.positionY,
                            v.Transformation.positionZ,
                            v.Transformation.rotationX,
                            v.Transformation.rotationY,
                            v.Transformation.rotationZ
                        }
                    })
                })
                .FirstOrDefaultAsync();

            if (farm == null) return NotFound();

            return Ok(farm);
        }
    }
}
