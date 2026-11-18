
using System.Security.Claims;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/employees/{employeeId:int}/attendance")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class AttendancePunchesController(
    IAttendancePunchService service
) : ControllerBase
{
    // ========================================
    // POST: Capture employee punch
    // ========================================

    [HttpPost("punches")]
    public async Task<IActionResult> Record(
        int employeeId,
        [FromBody] RecordAttendancePunchDto dto,
        CancellationToken ct)
    {
        if (employeeId <= 0 || dto.WorkDate == default)
        {
            return BadRequest(new
            {
                message = "Valid employeeId and workDate are required."
            });
        }

        var rawUserId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(rawUserId, out var actorUserId))
        {
            return Unauthorized(new
            {
                message = "Invalid authenticated user."
            });
        }

        try
        {
            var result = await service.RecordAsync(
                employeeId,
                actorUserId,
                dto,
                ct);

            return CreatedAtAction(
                nameof(List),
                new
                {
                    employeeId,
                    workDate = dto.WorkDate.ToString("yyyy-MM-dd")
                },
                result);
        }
        catch (SqlException ex)
            when (ex.Number >= 51401 && ex.Number <= 51407)
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // GET: Punches for a work date
    // ========================================

    [HttpGet("days/{workDate}/punches")]
    public async Task<IActionResult> List(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        if (workDate == default)
            return BadRequest(new
            {
                message = "Valid workDate is required."
            });

        var punches = await service.ListAsync(
            employeeId,
            workDate,
            ct);

        return Ok(punches);
    }

    // ========================================
    // GET: Attendance record and assessment
    // ========================================

    [HttpGet("days/{workDate}")]
    public async Task<IActionResult> GetDay(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        if (workDate == default)
            return BadRequest(new
            {
                message = "Valid workDate is required."
            });

        var result = await service.GetDayAsync(
            employeeId,
            workDate,
            ct);

        return result is null
            ? NotFound(new
              {
                  message = "No attendance record for this date."
              })
            : Ok(result);
    }

    // ========================================
    // SQL business error mapping
    // ========================================

    private IActionResult MapSqlError(SqlException ex)
    {
        var response = new { message = ex.Message };

        return ex.Number switch
        {
            51401 => NotFound(response),

            51404 or 51405 or 51406 =>
                Conflict(response),

            51407 => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                response),

            _ => BadRequest(response)
        };
    }
}
