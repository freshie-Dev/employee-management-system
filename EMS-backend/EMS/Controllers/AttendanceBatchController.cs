using System.Security.Claims;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/attendance/batches")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class AttendanceBatchController(
    IAttendanceBatchService service) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run(
        [FromBody] RunAttendanceBatchRequest request,
        CancellationToken ct)
    {
        if (request.WorkDate == default ||
            request.MaxEmployees is < 1 or > 5000)
            return BadRequest(new { message = "Valid workDate and maxEmployees are required." });

        var actorClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(actorClaim, out var actorUserId))
            return Unauthorized(new { message = "Invalid authenticated user." });

        try
        {
            var result = await service.RunAsync(
                request.WorkDate,
                request.DryRun,
                actorUserId,
                request.MaxEmployees,
                ct);
            return Ok(result);
        }
        catch (SqlException ex) when (ex.Number == 51501)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
