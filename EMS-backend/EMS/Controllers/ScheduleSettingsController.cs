using EMS.DTOs.Schedules;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/schedule-settings")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class ScheduleSettingsController(IScheduleSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await service.GetAsync(ct);
        return settings is null ? NotFound() : Ok(settings);
    }

    [HttpPut]
    public async Task<IActionResult> Update(UpdateScheduleSettingsDto dto, CancellationToken ct)
    {
        try { return Ok(await service.UpdateAsync(dto, ct)); }
        catch (SqlException ex) when (ex.Number is >= 51101 and <= 51104)
        { return BadRequest(new { message = ex.Message }); }
    }
}

[ApiController]
[Route("api/work-schedules")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class WorkSchedulesController(IScheduleSettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive, CancellationToken ct) =>
        Ok(await service.ListWorkSchedulesAsync(includeInactive, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateWorkScheduleDto dto, CancellationToken ct)
    {
        if (dto.WorkingDays is null || dto.WorkingDays.Any(d => d is < 1 or > 7)
            || dto.WorkingDays.Distinct().Count() != dto.WorkingDays.Count)
            return BadRequest(new { message = "Working days must be unique integers 1–7." });

        try
        {
            var schedule = await service.CreateWorkScheduleAsync(dto, ct);
            return StatusCode(StatusCodes.Status201Created, schedule);
        }
        catch (SqlException ex) when (ex.Number is 51108 or 2601 or 2627)
        { return Conflict(new { message = "Weekly schedule name already exists." }); }
        catch (SqlException ex) when (ex.Number is >= 51105 and <= 51107)
        { return BadRequest(new { message = ex.Message }); }
    }
}
