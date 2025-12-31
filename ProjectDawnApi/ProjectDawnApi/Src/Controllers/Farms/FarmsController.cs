using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectDawnApi.Src.Controllers.Farms
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmsController : ControllerBase
    {
        private readonly ProjectDawnDbContext context;

        public FarmsController(ProjectDawnDbContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetFarms()
        {
            var farms = await context.Farms
                .Include(f => f.Owner)
                .Include(f => f.Visitors)
                    .ThenInclude(v => v.PlayerDataModel) // 👈 include the right nav property
                .Select(f => new
                {
                    id = f.Id,
                    name = f.Name,
                    ownerName = f.Owner != null ? f.Owner.Name : "N/A",
                    visitors = f.Visitors.Select(v => new VisitorSummaryDM
                    {
                        playerId = v.PlayerId,
                        playerName = v.PlayerDataModel != null ? v.PlayerDataModel.Name : "Unknown"
                    }).ToList()
                })
                .ToListAsync();

            return Ok(farms);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFarm(int id)
        {
            var farm = await context.Farms
                .Include(f => f.Owner)
                .Include(f => f.PlacedObjects)
                .Include(f => f.Visitors)
                    .ThenInclude(v => v.PlayerDataModel)
                .Where(f => f.Id == id)
                .AsNoTracking()
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                    OwnerName = f.Owner != null ? f.Owner.Name : "N/A",
                   PlacedObjects = f.PlacedObjects.Select(po => new
                    {
                        id = po.Id,   // 👈 now a Guid
                        po.Type,
                        Transformation = new
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
                        Transformation = new
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
