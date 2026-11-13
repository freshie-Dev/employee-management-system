using EMS.DTOs.Holidays;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace EMS.Controllers;

[ApiController]
[Route("api/holiday-calendars")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public sealed class HolidayCalendarsController(IHolidayService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool includeInactive = false, CancellationToken ct = default) =>
        Ok(await service.ListCalendarsAsync(includeInactive, ct));

    [HttpGet("{calendarId:int}")]
    public async Task<IActionResult> Get(int calendarId, CancellationToken ct)
    {
        var item = await service.GetCalendarAsync(calendarId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveHolidayCalendarDto dto, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateCalendarAsync(dto, ct);
            return CreatedAtAction(nameof(Get), new { calendarId = item.CalendarId }, item);
        }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpPut("{calendarId:int}")]
    public async Task<IActionResult> Update(int calendarId, SaveHolidayCalendarDto dto, CancellationToken ct)
    {
        try { return Ok(await service.UpdateCalendarAsync(calendarId, dto, ct)); }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpPatch("{calendarId:int}/status")]
    public async Task<IActionResult> Status(int calendarId, SetHolidayCalendarStatusDto dto, CancellationToken ct)
    {
        try { return Ok(await service.SetCalendarStatusAsync(calendarId, dto.IsActive, ct)); }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpGet("{calendarId:int}/holidays")]
    public async Task<IActionResult> ListHolidays(int calendarId, [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate, CancellationToken ct)
    {
        try { return Ok(await service.ListHolidaysAsync(calendarId, fromDate, toDate, ct)); }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpGet("{calendarId:int}/holidays/{holidayId:int}")]
    public async Task<IActionResult> GetHoliday(int calendarId, int holidayId, CancellationToken ct)
    {
        var item = await service.GetHolidayAsync(calendarId, holidayId, ct);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost("{calendarId:int}/holidays")]
    public async Task<IActionResult> CreateHoliday(int calendarId, SaveHolidayDto dto, CancellationToken ct)
    {
        try
        {
            var item = await service.CreateHolidayAsync(calendarId, dto, ct);
            return CreatedAtAction(nameof(GetHoliday), new { calendarId, holidayId = item.HolidayId }, item);
        }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpPut("{calendarId:int}/holidays/{holidayId:int}")]
    public async Task<IActionResult> UpdateHoliday(int calendarId, int holidayId, SaveHolidayDto dto, CancellationToken ct)
    {
        try { return Ok(await service.UpdateHolidayAsync(calendarId, holidayId, dto, ct)); }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpDelete("{calendarId:int}/holidays/{holidayId:int}")]
    public async Task<IActionResult> DeleteHoliday(int calendarId, int holidayId, CancellationToken ct)
    {
        try
        {
            await service.DeleteHolidayAsync(calendarId, holidayId, ct);
            return NoContent();
        }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }

    [HttpGet("{calendarId:int}/check-date")]
    public async Task<IActionResult> CheckDate(int calendarId, [FromQuery] DateOnly? workDate, CancellationToken ct)
    {
        if (workDate is null) return BadRequest(new { message = "workDate is required." });
        try { return Ok(await service.CheckDateAsync(calendarId, workDate.Value, ct)); }
        catch (SqlException ex) when (HolidaySqlErrors.Known(ex)) { return HolidaySqlErrors.ToAction(this, ex); }
    }
}

internal static class HolidaySqlErrors
{
    public static bool Known(SqlException ex) => ex.Number is >= 51401 and <= 51405 or 2601 or 2627;

    public static IActionResult ToAction(ControllerBase controller, SqlException ex)
    {
        var result = new { message = ex.Message };
        return ex.Number switch
        {
            51401 or 51402 => controller.NotFound(result),
            51405 or 2601 or 2627 => controller.Conflict(result),
            51403 or 51404 => controller.BadRequest(result),
            _ => controller.StatusCode(500)
        };
    }
}
