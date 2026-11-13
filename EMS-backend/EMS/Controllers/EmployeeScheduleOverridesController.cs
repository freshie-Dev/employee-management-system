
using System.Security.Claims;
using EMS.DTOs.ScheduleOverrides;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/employees/{employeeId:int}")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class EmployeeScheduleOverridesController(
    IScheduleOverrideService service
) : ControllerBase
{
    // =========================================
    // GET: Employee overrides
    // =========================================

    [HttpGet("schedule-overrides")]
    public async Task<IActionResult> List(
        int employeeId,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] bool includeCancelled,
        CancellationToken ct)
    {
        var result = await service.ListAsync(
            employeeId,
            fromDate,
            toDate,
            includeCancelled,
            ct
        );

        return Ok(result);
    }

    // =========================================
    // GET: Override by ID
    // =========================================

    [HttpGet("schedule-overrides/{overrideId:long}")]
    public async Task<IActionResult> Get(
        int employeeId,
        long overrideId,
        CancellationToken ct)
    {
        var result = await service.GetAsync(
            employeeId,
            overrideId,
            ct
        );

        return result is null
            ? NotFound()
            : Ok(result);
    }

    // =========================================
    // GET: Resolved schedule
    // =========================================

    [HttpGet("schedule")]
    public async Task<IActionResult> Resolve(
        int employeeId,
        [FromQuery] DateOnly workDate,
        CancellationToken ct)
    {
        if (workDate == default)
        {
            return BadRequest(new
            {
                message = "workDate is required."
            });
        }

        var result = await service.ResolveAsync(
            employeeId,
            workDate,
            ct
        );

        return Ok(result);
    }

    // =========================================
    // POST: Create override
    // =========================================

    [HttpPost("schedule-overrides")]
    public async Task<IActionResult> Create(
        int employeeId,
        CreateScheduleOverrideDto dto,
        CancellationToken ct)
    {
        if (dto.WorkDate == default)
        {
            return BadRequest(new
            {
                message = "workDate is required."
            });
        }

        var raw = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        if (!int.TryParse(raw, out var actorId))
        {
            return Unauthorized(new
            {
                message = "JWT must contain numeric UserId."
            });
        }

        try
        {
            var result = await service.CreateAsync(
                employeeId,
                actorId,
                dto,
                ct
            );

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    employeeId,
                    overrideId = result.OverrideId
                },
                result
            );
        }
        catch (SqlException ex)
            when (ex.Number is >= 51301 and <= 51307)
        {
            return SqlError(ex);
        }
    }

    // =========================================
    // PUT: Update override
    // =========================================

    [HttpPut("schedule-overrides/{overrideId:long}")]
    public async Task<IActionResult> Update(
        int employeeId,
        long overrideId,
        UpdateScheduleOverrideDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await service.UpdateAsync(
                employeeId,
                overrideId,
                dto,
                ct
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (SqlException ex)
            when (ex.Number is >= 51301 and <= 51307)
        {
            return SqlError(ex);
        }
    }

    // =========================================
    // PATCH: Cancel override
    // =========================================

    [HttpPatch(
        "schedule-overrides/{overrideId:long}/cancel")]
    public async Task<IActionResult> Cancel(
        int employeeId,
        long overrideId,
        CancelScheduleOverrideDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await service.CancelAsync(
                employeeId,
                overrideId,
                dto,
                ct
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new
            {
                message = ex.Message
            });
        }
        catch (SqlException ex)
            when (ex.Number is >= 51301 and <= 51307)
        {
            return SqlError(ex);
        }
    }

    // =========================================
    // Database error mapping
    // =========================================

    private IActionResult SqlError(SqlException ex)
    {
        var response = new
        {
            message = ex.Message
        };

        return ex.Number switch
        {
            51301 or 51302 => NotFound(response),

            51304 or 51305 or 51306 =>
                Conflict(response),

            51307 => Forbid(),

            _ => BadRequest(response)
        };
    }
}
