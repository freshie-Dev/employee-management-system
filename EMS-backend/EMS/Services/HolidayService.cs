using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Holidays;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class HolidayService(IStoredProcedureExecutor executor) : IHolidayService
{
    private static void Id(SqlParameterCollection p, string name, int value) =>
        p.Add(name, SqlDbType.Int).Value = value;

    private static void Date(SqlParameterCollection p, string name, DateOnly? value) =>
        p.Add(name, SqlDbType.Date).Value = value.HasValue
            ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;

    private static void Name(SqlParameterCollection p, string name, string value, int length) =>
        p.Add(name, SqlDbType.NVarChar, length).Value = value.Trim();

    private static HolidayCalendarDto MapCalendar(SqlDataReader r) => new(
        r.GetInt32(r.GetOrdinal("CalendarId")),
        r.GetString(r.GetOrdinal("Name")),
        r.GetBoolean(r.GetOrdinal("IsActive")),
        r.GetInt32(r.GetOrdinal("HolidayCount")),
        r.GetBoolean(r.GetOrdinal("IsDefault")));

    private static HolidayDto MapHoliday(SqlDataReader r) => new(
        r.GetInt32(r.GetOrdinal("HolidayId")),
        r.GetInt32(r.GetOrdinal("CalendarId")),
        DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("HolidayDate"))),
        r.GetString(r.GetOrdinal("Name")));

    private static HolidayDateCheckDto MapCheck(SqlDataReader r) => new(
        r.GetInt32(r.GetOrdinal("CalendarId")),
        DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("WorkDate"))),
        r.GetBoolean(r.GetOrdinal("IsHoliday")),
        r.IsDBNull(r.GetOrdinal("HolidayId")) ? null : r.GetInt32(r.GetOrdinal("HolidayId")),
        r.IsDBNull(r.GetOrdinal("HolidayName")) ? null : r.GetString(r.GetOrdinal("HolidayName")));

    private static T Required<T>(T? row, string procedure) where T : class =>
        row ?? throw new InvalidOperationException($"{procedure} returned no row.");

    public async Task<IReadOnlyList<HolidayCalendarDto>> ListCalendarsAsync(bool includeInactive, CancellationToken ct) =>
        await executor.QueryAsync("dbo.usp_HolidayCalendar_List",
            p => p.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive, MapCalendar, ct);

    public Task<HolidayCalendarDto?> GetCalendarAsync(int id, CancellationToken ct) =>
        executor.QuerySingleAsync("dbo.usp_HolidayCalendar_GetById", p => Id(p, "@CalendarId", id), MapCalendar, ct);

    public async Task<HolidayCalendarDto> CreateCalendarAsync(SaveHolidayCalendarDto dto, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_HolidayCalendar_Create",
            p => Name(p, "@Name", dto.Name, 100), MapCalendar, ct), "usp_HolidayCalendar_Create");

    public async Task<HolidayCalendarDto> UpdateCalendarAsync(int id, SaveHolidayCalendarDto dto, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_HolidayCalendar_Update", p => {
            Id(p, "@CalendarId", id); Name(p, "@Name", dto.Name, 100);
        }, MapCalendar, ct), "usp_HolidayCalendar_Update");

    public async Task<HolidayCalendarDto> SetCalendarStatusAsync(int id, bool isActive, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_HolidayCalendar_SetStatus", p => {
            Id(p, "@CalendarId", id);
            p.Add("@IsActive", SqlDbType.Bit).Value = isActive;
        }, MapCalendar, ct), "usp_HolidayCalendar_SetStatus");

    public async Task<IReadOnlyList<HolidayDto>> ListHolidaysAsync(int calendarId, DateOnly? from, DateOnly? to, CancellationToken ct) =>
        await executor.QueryAsync("dbo.usp_Holiday_List", p => {
            Id(p, "@CalendarId", calendarId); Date(p, "@FromDate", from); Date(p, "@ToDate", to);
        }, MapHoliday, ct);

    public Task<HolidayDto?> GetHolidayAsync(int calendarId, int holidayId, CancellationToken ct) =>
        executor.QuerySingleAsync("dbo.usp_Holiday_GetById", p => {
            Id(p, "@CalendarId", calendarId); Id(p, "@HolidayId", holidayId);
        }, MapHoliday, ct);

    public async Task<HolidayDto> CreateHolidayAsync(int calendarId, SaveHolidayDto dto, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_Holiday_Create", p => {
            Id(p, "@CalendarId", calendarId);
            Date(p, "@HolidayDate", dto.HolidayDate);
            Name(p, "@Name", dto.Name, 150);
        }, MapHoliday, ct), "usp_Holiday_Create");

    public async Task<HolidayDto> UpdateHolidayAsync(int calendarId, int holidayId, SaveHolidayDto dto, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_Holiday_Update", p => {
            Id(p, "@CalendarId", calendarId); Id(p, "@HolidayId", holidayId);
            Date(p, "@HolidayDate", dto.HolidayDate);
            Name(p, "@Name", dto.Name, 150);
        }, MapHoliday, ct), "usp_Holiday_Update");

    public async Task DeleteHolidayAsync(int calendarId, int holidayId, CancellationToken ct) =>
        _ = await executor.ExecuteAsync("dbo.usp_Holiday_Delete", p => {
            Id(p, "@CalendarId", calendarId); Id(p, "@HolidayId", holidayId);
        }, ct);

    public async Task<HolidayDateCheckDto> CheckDateAsync(int calendarId, DateOnly workDate, CancellationToken ct) =>
        Required(await executor.QuerySingleAsync("dbo.usp_Holiday_CheckDate", p => {
            Id(p, "@CalendarId", calendarId); Date(p, "@WorkDate", workDate);
        }, MapCheck, ct), "usp_Holiday_CheckDate");
}
