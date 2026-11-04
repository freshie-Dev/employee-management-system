using EMS.DTOs.EmployeeShifts;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/employees/{employeeId:int}/shifts")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class EmployeeShiftsController(IEmployeeShiftService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(int employeeId, CancellationToken ct)
    {
        try { return Ok(await service.ListAsync(employeeId, ct)); }
        catch (SqlException ex) when (ex.Number == 51201) { return NotFound(new { message = ex.Message }); }
    }

    [HttpGet("effective")]
    public async Task<IActionResult> GetEffective(int employeeId, [FromQuery] DateOnly workDate, CancellationToken ct)
    {
        if (workDate == default) return BadRequest(new { message = "workDate is required (YYYY-MM-DD)." });
        try
        {
            var row = await service.GetEffectiveAsync(employeeId, workDate, ct);
            return row is null ? NotFound(new { message = "No regular shift assigned for this date." }) : Ok(row);
        }
        catch (SqlException ex) when (ex.Number == 51201) { return NotFound(new { message = ex.Message }); }
    }

    [HttpPost]
    public async Task<IActionResult> Assign(int employeeId, AssignEmployeeShiftDto dto, CancellationToken ct)
    {
        if (dto.EffectiveFrom == default) return BadRequest(new { message = "effectiveFrom is required." });
        try { return Ok(await service.AssignAsync(employeeId, dto, ct)); }
        catch (SqlException ex) when (ex.Number == 51201) { return NotFound(new { message = ex.Message }); }
        catch (SqlException ex) when (ex.Number is 51204 or 2601 or 2627) { return Conflict(new { message = ex.Message }); }
        catch (SqlException ex) when (ex.Number is 51202 or 51203) { return BadRequest(new { message = ex.Message }); }
        catch (SqlException ex) when (ex.Number == 51210) { return StatusCode(503, new { message = "Shift assignment is busy. Retry the request." }); }
    }

    [HttpPost("initialize-default")]
    public async Task<IActionResult> InitializeDefault(int employeeId, CancellationToken ct)
    {
        try { return Ok(await service.InitializeDefaultAsync(employeeId, ct)); }
        catch (SqlException ex) when (ex.Number == 51201) { return NotFound(new { message = ex.Message }); }
        catch (SqlException ex) when (ex.Number == 51205) { return BadRequest(new { message = ex.Message }); }
    }
}
