using System.Data;
using System.Text.Json;
using EMS.DB.SPs;
using EMS.DTOs.Schedules;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class ScheduleSettingsService(IStoredProcedureExecutor executor) : IScheduleSettingsService
{
    private static ScheduleSettingsDto MapSettings(SqlDataReader r)
    {
        int? NullableInt(string name) => r.IsDBNull(r.GetOrdinal(name)) ? null : r.GetInt32(r.GetOrdinal(name));
        string? NullableString(string name) => r.IsDBNull(r.GetOrdinal(name)) ? null : r.GetString(r.GetOrdinal(name));

        return new ScheduleSettingsDto(
            r.GetByte(r.GetOrdinal("SettingsId")),
            NullableInt("DefaultShiftId"),
            NullableString("DefaultShiftName"),
            NullableInt("DefaultScheduleId"),
            NullableString("DefaultScheduleName"),
            NullableInt("DefaultHolidayCalendarId"),
            r.GetDateTime(r.GetOrdinal("UpdatedAt")));
    }

    private sealed record ScheduleRow(
        int Id, string Name, bool IsActive, DateTime CreatedAt,
        byte? DayOfWeek, bool? IsWorkingDay);

    private static ScheduleRow MapScheduleRow(SqlDataReader r)
    {
        int day = r.GetOrdinal("DayOfWeek");
        int working = r.GetOrdinal("IsWorkingDay");
        return new ScheduleRow(
            r.GetInt32(r.GetOrdinal("ScheduleId")),
            r.GetString(r.GetOrdinal("Name")),
            r.GetBoolean(r.GetOrdinal("IsActive")),
            r.GetDateTime(r.GetOrdinal("CreatedAt")),
            r.IsDBNull(day) ? null : r.GetByte(day),
            r.IsDBNull(working) ? null : r.GetBoolean(working));
    }

    private static List<WorkScheduleDto> GroupSchedules(List<ScheduleRow> rows) =>
        rows.GroupBy(r => r.Id).Select(g =>
        {
            var first = g.First();
            return new WorkScheduleDto(
                first.Id, first.Name, first.IsActive, first.CreatedAt,
                g.Where(x => x.DayOfWeek.HasValue)
                    .Select(x => new WorkScheduleDayDto(x.DayOfWeek!.Value, x.IsWorkingDay ?? false))
                    .OrderBy(x => x.DayOfWeek).ToList());
        }).ToList();

    public Task<ScheduleSettingsDto?> GetAsync(CancellationToken ct) =>
        executor.QuerySingleAsync("dbo.usp_ScheduleSettings_Get", null, MapSettings, ct);

    public async Task<ScheduleSettingsDto> UpdateAsync(UpdateScheduleSettingsDto dto, CancellationToken ct)
    {
        var updated = await executor.QuerySingleAsync(
            "dbo.usp_ScheduleSettings_Update",
            p =>
            {
                p.Add("@DefaultShiftId", SqlDbType.Int).Value = dto.DefaultShiftId;
                p.Add("@DefaultScheduleId", SqlDbType.Int).Value = dto.DefaultScheduleId;
                p.Add("@DefaultHolidayCalendarId", SqlDbType.Int).Value =
                    (object?)dto.DefaultHolidayCalendarId ?? DBNull.Value;
            }, MapSettings, ct);

        return updated ?? throw new InvalidOperationException("Settings update returned no row.");
    }

    public async Task<IReadOnlyList<WorkScheduleDto>> ListWorkSchedulesAsync(
        bool includeInactive, CancellationToken ct)
    {
        var rows = await executor.QueryAsync("dbo.usp_WorkSchedule_List",
            p => p.Add("@IncludeInactive", SqlDbType.Bit).Value = includeInactive,
            MapScheduleRow, ct);
        return GroupSchedules(rows);
    }

    public async Task<WorkScheduleDto> CreateWorkScheduleAsync(
    CreateWorkScheduleDto dto,
    CancellationToken ct)
    {
        if (dto.WorkingDays is null ||
            dto.WorkingDays.Any(d => d is < 1 or > 7) ||
            dto.WorkingDays.Distinct().Count() != dto.WorkingDays.Count)
        {
            throw new ArgumentException(
                "Working days must be distinct values 1–7.");
        }

        int createdScheduleId = 0;

        await executor.ExecuteWithOutputAsync(
            "dbo.usp_WorkSchedule_Create",
            p =>
            {
                p.Add("@Name", SqlDbType.NVarChar, 100).Value =
                    dto.Name.Trim();

                p.Add("@WorkingDaysJson", SqlDbType.NVarChar, -1).Value =
                    JsonSerializer.Serialize(dto.WorkingDays);

                p.Add("@ScheduleId", SqlDbType.Int).Direction =
                    ParameterDirection.Output;
            },
            MapScheduleRow,
            (firstRow, parameters) =>
            {
                createdScheduleId = Convert.ToInt32(
                    parameters["@ScheduleId"].Value);

                if (firstRow is null)
                    throw new InvalidOperationException(
                        $"Created schedule {createdScheduleId} but no row returned.");

                return firstRow;
            },
            ct);

        var schedules = await ListWorkSchedulesAsync(true, ct);

        return schedules.Single(
            s => s.ScheduleId == createdScheduleId);
    }
}
