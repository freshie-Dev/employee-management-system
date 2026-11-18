
using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.ScheduleOverrides;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class ScheduleOverrideService(
    IStoredProcedureExecutor executor
) : IScheduleOverrideService
{
    // =========================================
    // SQL parameter helpers
    // =========================================

    private static void Int(
        SqlParameterCollection p,
        string name,
        int value)
    {
        p.Add(name, SqlDbType.Int).Value = value;
    }

    private static void Big(
        SqlParameterCollection p,
        string name,
        long value)
    {
        p.Add(name, SqlDbType.BigInt).Value = value;
    }

    private static void Date(
        SqlParameterCollection p,
        string name,
        DateOnly? value)
    {
        p.Add(name, SqlDbType.Date).Value =
            value.HasValue
                ? value.Value.ToDateTime(TimeOnly.MinValue)
                : DBNull.Value;
    }

    private static void OptDate(
        SqlParameterCollection p,
        string name,
        DateTimeOffset? value)
    {
        p.Add(name, SqlDbType.DateTimeOffset).Value =
            value.HasValue
                ? value.Value
                : DBNull.Value;
    }

    private static void Policy(
        SqlParameterCollection p,
        string overrideType,
        DateTimeOffset? start,
        DateTimeOffset? end,
        int breakMinutes,
        int graceIn,
        int graceOut,
        int? requiredMinutes,
        string? reason)
    {
        p.Add(
            "@OverrideType",
            SqlDbType.VarChar, 12
        ).Value = overrideType;

        OptDate(p, "@StartAt", start);
        OptDate(p, "@EndAt", end);

        Int(p, "@BreakMinutes", breakMinutes);
        Int(p, "@GraceInMinutes", graceIn);
        Int(p, "@GraceOutMinutes", graceOut);

        p.Add(
            "@RequiredMinutes",
            SqlDbType.Int
        ).Value = requiredMinutes.HasValue
            ? requiredMinutes.Value
            : DBNull.Value;

        p.Add(
            "@Reason",
            SqlDbType.NVarChar, 500
        ).Value = reason is null
            ? DBNull.Value
            : reason;
    }

    // =========================================
    // RowVersion handling
    // =========================================

    private static byte[] ParseVersion(string input)
    {
        try
        {
            var bytes = Convert.FromBase64String(input);

            if (bytes.Length == 8)
                return bytes;
        }
        catch (FormatException)
        {
            // Return a validation error below.
        }

        throw new ArgumentException(
            "rowVersion must be a base64-encoded " +
            "8-byte SQL rowversion.");
    }

    // =========================================
    // Map schedule override
    // =========================================

    private static ScheduleOverrideDto Map(
        SqlDataReader r)
    {
        T? Nullable<T>(
            string name,
            Func<int, T> get)
            where T : struct
        {
            int i = r.GetOrdinal(name);

            return r.IsDBNull(i)
                ? null
                : get(i);
        }

        string? Str(string name)
        {
            int i = r.GetOrdinal(name);

            return r.IsDBNull(i)
                ? null
                : r.GetString(i);
        }

        return new ScheduleOverrideDto(
            r.GetInt64(r.GetOrdinal("OverrideId")),
            r.GetInt32(r.GetOrdinal("EmployeeId")),
            DateOnly.FromDateTime(
                r.GetDateTime(r.GetOrdinal("WorkDate"))),
            r.GetString(r.GetOrdinal("OverrideType")),
            Nullable("StartAt", r.GetDateTimeOffset),
            Nullable("EndAt", r.GetDateTimeOffset),
            r.GetInt32(r.GetOrdinal("BreakMinutes")),
            r.GetInt32(r.GetOrdinal("GraceInMinutes")),
            r.GetInt32(r.GetOrdinal("GraceOutMinutes")),
            Nullable("RequiredMinutes", r.GetInt32),
            Str("Reason"),
            r.GetInt32(r.GetOrdinal("AssignedByUserId")),
            r.GetString(r.GetOrdinal("RecordStatus")),
            r.GetDateTime(r.GetOrdinal("CreatedAt")),
            r.GetDateTime(r.GetOrdinal("UpdatedAt")),
            Convert.ToBase64String(
                (byte[])r["RowVersion"])
        );
    }

    // =========================================
    // Map resolved schedule
    // =========================================


    private static ResolvedEmployeeScheduleDto MapResolved(
        SqlDataReader reader)
    {
        // ========================================
        // Nullable value reader
        // ========================================

        T? GetNullable<T>(
            string column,
            Func<int, T> getValue)
            where T : struct
        {
            int index = reader.GetOrdinal(column);

            return reader.IsDBNull(index)
                ? null
                : getValue(index);
        }

        // ========================================
        // Nullable string reader
        // ========================================

        string? GetNullableString(string column)
        {
            int index = reader.GetOrdinal(column);

            return reader.IsDBNull(index)
                ? null
                : reader.GetString(index);
        }

        // ========================================
        // Map SQL result to DTO
        // ========================================

        return new ResolvedEmployeeScheduleDto(
            EmployeeId: reader.GetInt32(
                reader.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                reader.GetDateTime(
                    reader.GetOrdinal("WorkDate"))),

            ScheduleSource: reader.GetString(
                reader.GetOrdinal("ScheduleSource")),

            OverrideId: GetNullable(
                "OverrideId",
                reader.GetInt64),

            ShiftId: GetNullable(
                "ShiftId",
                reader.GetInt32),

            OverrideType: GetNullableString(
                "OverrideType"),

            IsScheduled: reader.GetBoolean(
                reader.GetOrdinal("IsScheduled")),

            ScheduledStartAt: GetNullable(
                "ScheduledStartAt",
                reader.GetDateTimeOffset),

            ScheduledEndAt: GetNullable(
                "ScheduledEndAt",
                reader.GetDateTimeOffset),

            BreakMinutes: GetNullable(
                "BreakMinutes",
                reader.GetInt32),

            GraceInMinutes: GetNullable(
                "GraceInMinutes",
                reader.GetInt32),

            GraceOutMinutes: GetNullable(
                "GraceOutMinutes",
                reader.GetInt32),

            RequiredMinutes: GetNullable(
                "RequiredMinutes",
                reader.GetInt32),

            IsHoliday: reader.GetBoolean(
                reader.GetOrdinal("IsHoliday")),

            HolidayName: GetNullableString(
                "HolidayName"),

            IsWeekend: reader.GetBoolean(
                reader.GetOrdinal("IsWeekend")),

            IsWeeklyWorkingDay: reader.GetBoolean(
                reader.GetOrdinal("IsWeeklyWorkingDay")),

            IsSpecialWorkDay: reader.GetBoolean(
                reader.GetOrdinal("IsSpecialWorkDay")),

            UsedDefaultShift: reader.GetBoolean(
                reader.GetOrdinal("UsedDefaultShift")),

            HolidayCalendarId: GetNullable(
                "HolidayCalendarId",
                reader.GetInt32)
        );
    }

    // =========================================
    // List overrides
    // =========================================

    public async Task<IReadOnlyList<ScheduleOverrideDto>> ListAsync(
        int employeeId,
        DateOnly? from,
        DateOnly? to,
        bool includeCancelled,
        CancellationToken ct)
    {
        return await executor.QueryAsync(
            "dbo.usp_ScheduleOverride_ListByEmployee",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Date(p, "@FromDate", from);
                Date(p, "@ToDate", to);

                p.Add(
                    "@IncludeCancelled",
                    SqlDbType.Bit
                ).Value = includeCancelled;
            },
            Map,
            ct
        );
    }

    // =========================================
    // Get override by ID
    // =========================================

    public Task<ScheduleOverrideDto?> GetAsync(
        int employeeId,
        long overrideId,
        CancellationToken ct)
    {
        return executor.QuerySingleAsync(
            "dbo.usp_ScheduleOverride_GetById",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Big(p, "@OverrideId", overrideId);
            },
            Map,
            ct
        );
    }

    // =========================================
    // Create override
    // =========================================

    public async Task<ScheduleOverrideDto> CreateAsync(
        int employeeId,
        int actorUserId,
        CreateScheduleOverrideDto dto,
        CancellationToken ct)
    {
        var row = await executor.QuerySingleAsync(
            "dbo.usp_ScheduleOverride_Create",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Date(p, "@WorkDate", dto.WorkDate);

                Policy(
                    p,
                    dto.OverrideType,
                    dto.StartAt,
                    dto.EndAt,
                    dto.BreakMinutes,
                    dto.GraceInMinutes,
                    dto.GraceOutMinutes,
                    dto.RequiredMinutes,
                    dto.Reason
                );

                Int(
                    p,
                    "@AssignedByUserId",
                    actorUserId
                );
            },
            Map,
            ct
        );

        return row ?? throw new InvalidOperationException(
            "Create procedure returned no row.");
    }

    // =========================================
    // Update override
    // =========================================

    public async Task<ScheduleOverrideDto> UpdateAsync(
        int employeeId,
        long overrideId,
        UpdateScheduleOverrideDto dto,
        CancellationToken ct)
    {
        var row = await executor.QuerySingleAsync(
            "dbo.usp_ScheduleOverride_Update",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Big(p, "@OverrideId", overrideId);

                p.Add(
                    "@ExpectedRowVersion",
                    SqlDbType.Binary, 8
                ).Value = ParseVersion(dto.RowVersion);

                Policy(
                    p,
                    dto.OverrideType,
                    dto.StartAt,
                    dto.EndAt,
                    dto.BreakMinutes,
                    dto.GraceInMinutes,
                    dto.GraceOutMinutes,
                    dto.RequiredMinutes,
                    dto.Reason
                );
            },
            Map,
            ct
        );

        return row ?? throw new InvalidOperationException(
            "Update procedure returned no row.");
    }

    // =========================================
    // Cancel override
    // =========================================

    public async Task<ScheduleOverrideDto> CancelAsync(
        int employeeId,
        long overrideId,
        CancelScheduleOverrideDto dto,
        CancellationToken ct)
    {
        var row = await executor.QuerySingleAsync(
            "dbo.usp_ScheduleOverride_Cancel",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Big(p, "@OverrideId", overrideId);

                p.Add(
                    "@ExpectedRowVersion",
                    SqlDbType.Binary, 8
                ).Value = ParseVersion(dto.RowVersion);
            },
            Map,
            ct
        );

        return row ?? throw new InvalidOperationException(
            "Cancel procedure returned no row.");
    }

    // =========================================
    // Resolve employee schedule
    // =========================================

    public async Task<ResolvedEmployeeScheduleDto> ResolveAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        var row = await executor.QuerySingleAsync(
            "dbo.usp_EmployeeSchedule_Resolve",
            p =>
            {
                Int(p, "@EmployeeId", employeeId);
                Date(p, "@WorkDate", workDate);
            },
            MapResolved,
            ct
        );

        return row ?? throw new InvalidOperationException(
            "Schedule resolution returned no row.");
    }
}
