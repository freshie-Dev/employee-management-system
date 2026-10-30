
using EMS.DB;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "SYSTEM_ADMIN")]
public class RolesController(AppDbContext db)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        CancellationToken ct)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.RoleId,
                r.Name,
                r.Description
            })
            .ToListAsync(ct);

        return Ok(roles);
    }
}
