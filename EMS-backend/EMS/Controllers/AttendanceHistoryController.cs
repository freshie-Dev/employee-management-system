
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
public sealed class AttendanceHistoryController(
    IAttendanceHistoryService service
) : ControllerBase
{
    // ==========================================
    // GET: Attendance history
    // ==========================================

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        int employeeId,
        [FromQuery] DateOnly fromDate,
        [FromQuery] DateOnly toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (employeeId <= 0
            || fromDate == default
            || toDate == default
            || fromDate > toDate
            || toDate.DayNumber - fromDate.DayNumber > 366
            || page < 1 || page > 100000
            || pageSize < 1 || pageSize > 100)
        {
            return BadRequest(new
            {
                message = "Invalid date range or pagination."
            });
        }

        var result = await service.GetHistoryAsync(
            employeeId,
            fromDate,
            toDate,
            page,
            pageSize,
            ct);

        return Ok(result);
    }

    // ==========================================
    // POST: Correct attendance punch
    // ==========================================

    [HttpPost("days/{workDate}/punch-corrections")]
    public async Task<IActionResult> CorrectPunch(
        int employeeId,
        DateOnly workDate,
        [FromBody] CorrectAttendancePunchRequest request,
        CancellationToken ct)
    {
        if (employeeId <= 0 || workDate == default)
        {
            return BadRequest(new
            {
                message = "Invalid employee or work date."
            });
        }

        if (request.Action is "INSERT" or "UPDATE")
        {
            if (request.PunchAtUtc is null
                || request.PunchType is not ("IN" or "OUT"))
            {
                return BadRequest(new
                {
                    message = "New punch type and UTC timestamp are required."
                });
            }
        }

        if (request.Action is "UPDATE" or "DELETE")
        {
            if (request.PunchId is null
                || request.ExpectedPunchAtUtc is null
                || request.ExpectedPunchType is not ("IN" or "OUT"))
            {
                return BadRequest(new
                {
                    message = "Original punch values are required."
                });
            }
        }

        if ((request.PunchAtUtc.HasValue
                && request.PunchAtUtc.Value.Offset != TimeSpan.Zero)
            || (request.ExpectedPunchAtUtc.HasValue
                && request.ExpectedPunchAtUtc.Value.Offset != TimeSpan.Zero))
        {
            return BadRequest(new
            {
                message = "Punch timestamps must use UTC (Z)."
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
            var result = await service.CorrectPunchAsync(
                employeeId,
                workDate,
                actorUserId,
                request,
                ct);

            return Ok(result);
        }
        catch (SqlException ex)
            when (ex.Number >= 51401 && ex.Number <= 51411)
        {
            return MapSqlError(ex);
        }
    }

    private IActionResult MapSqlError(SqlException ex)
    {
        var response = new { message = ex.Message };

        return ex.Number switch
        {
            51401 or 51408 => NotFound(response),

            51404 or 51405 or 51406 => Conflict(response),

            51407 => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                response),

            51411 => StatusCode(
                StatusCodes.Status500InternalServerError,
                new { message = "Attendance configuration is incomplete." }),

            _ => BadRequest(response)
        };
    }
}
