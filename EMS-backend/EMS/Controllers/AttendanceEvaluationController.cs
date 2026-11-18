
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
public sealed class AttendanceEvaluationController(
    IAttendanceEvaluationService service
) : ControllerBase
{
    // ========================================
    // POST: Evaluate or finalize attendance
    // ========================================

    [HttpPost("days/{workDate}/evaluate")]
    public async Task<IActionResult> Evaluate(
        int employeeId,
        DateOnly workDate,
        [FromBody] EvaluateAttendanceRequest request,
        CancellationToken ct)
    {
        if (employeeId <= 0 || workDate == default)
        {
            return BadRequest(new
            {
                message = "Invalid employee or work date."
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
            var result = await service.EvaluateAsync(
                employeeId,
                workDate,
                request.FinalizeAttendance,
                actorUserId,
                ct);

            return Ok(result);
        }
        catch (SqlException ex)
            when (ex.Number >= 51401 && ex.Number <= 51411)
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // GET: Attendance exceptions
    // ========================================

    [HttpGet("days/{workDate}/exceptions")]
    public async Task<IActionResult> GetExceptions(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        if (employeeId <= 0 || workDate == default)
        {
            return BadRequest(new
            {
                message = "Invalid employee or work date."
            });
        }

        var exceptions = await service.GetExceptionsAsync(
            employeeId,
            workDate,
            ct);

        return Ok(exceptions);
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

            51406 or 51408 or 51409 =>
                Conflict(response),

            51407 => StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                response),

            51411 => StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    message = "Required attendance configuration is missing."
                }),

            _ => BadRequest(response)
        };
    }
}
