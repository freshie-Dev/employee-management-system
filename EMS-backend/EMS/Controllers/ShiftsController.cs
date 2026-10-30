using EMS.DTOs.Shifts;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/shifts")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class ShiftsController(IShiftService shifts)
    : ControllerBase
{
    // ========================================
    // GET /api/shifts
    // ========================================

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool includeInactive = false,
        CancellationToken ct = default)
    {
        var result = await shifts.GetAllAsync(
            includeInactive,
            ct);

        return Ok(result);
    }

    // ========================================
    // GET /api/shifts/{id}
    // ========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken ct)
    {
        var result = await shifts.GetByIdAsync(id, ct);

        return result is null
            ? NotFound()
            : Ok(result);
    }

    // ========================================
    // POST /api/shifts
    // ========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateShiftDto dto,
        CancellationToken ct)
    {
        try
        {
            var result = await shifts.CreateAsync(dto, ct);

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.ShiftId },
                result);
        }
        catch (SqlException ex) when (IsBusinessError(ex))
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // PUT /api/shifts/{id}
    // ========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateShiftDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await shifts.UpdateAsync(
                id,
                dto,
                ct));
        }
        catch (SqlException ex) when (IsBusinessError(ex))
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // PATCH /api/shifts/{id}/status
    // ========================================

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> SetStatus(
        int id,
        UpdateShiftStatusDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await shifts.SetStatusAsync(
                id,
                dto.IsActive,
                ct));
        }
        catch (SqlException ex) when (IsBusinessError(ex))
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // PUT /api/shifts/{id}/policy
    // ========================================

    [HttpPut("{id:int}/policy")]
    public async Task<IActionResult> UpdatePolicy(
        int id,
        ShiftPolicyDto dto,
        CancellationToken ct)
    {
        try
        {
            return Ok(await shifts.UpdatePolicyAsync(
                id,
                dto,
                ct));
        }
        catch (SqlException ex) when (IsBusinessError(ex))
        {
            return MapSqlError(ex);
        }
    }

    // ========================================
    // SQL error handling
    // ========================================

    private static bool IsBusinessError(SqlException ex) =>
        ex.Number is >= 51001 and <= 51013
            or 2601
            or 2627;

    private IActionResult MapSqlError(SqlException ex)
    {
        var response = new { message = ex.Message };

        return ex.Number switch
        {
            // Shift not found
            51010 => NotFound(response),

            // Shift already assigned or historical
            51011 => Conflict(response),

            // Default shift cannot be deactivated
            51013 => Conflict(response),

            // Unique constraint violations
            2601 or 2627 => Conflict(response),

            // Other stored procedure validation errors
            >= 51001 and <= 51012 => BadRequest(response),

            _ => StatusCode(500, new
            {
                message = "Unexpected database error."
            })
        };
    }
}