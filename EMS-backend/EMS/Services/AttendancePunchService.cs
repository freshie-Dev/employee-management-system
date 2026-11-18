
using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class AttendancePunchService(
    IStoredProcedureExecutor executor
) : IAttendancePunchService
{
    // ========================================
    // SQL parameter helpers
    // ========================================

    private static void AddInt(
        SqlParameterCollection p,
        string name,
        int value)
    {
        p.Add(name, SqlDbType.Int).Value = value;
    }

    private static void AddDate(
        SqlParameterCollection p,
        string name,
        DateOnly value)
    {
        p.Add(name, SqlDbType.Date).Value =
            value.ToDateTime(TimeOnly.MinValue);
    }

    // ========================================
    // Row mapping: Attendance punch
    // ========================================

    private static AttendancePunchDto MapPunch(
        SqlDataReader r)
    {
        var timestamp = r.GetDateTime(
            r.GetOrdinal("PunchAtUtc"));

        return new AttendancePunchDto(
            PunchId: r.GetInt32(
                r.GetOrdinal("PunchId")),

            AttendanceId: r.GetInt32(
                r.GetOrdinal("AttendanceId")),

            EmployeeId: r.GetInt32(
                r.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                r.GetDateTime(
                    r.GetOrdinal("WorkDate"))),

            PunchType: r.GetString(
                r.GetOrdinal("PunchType")),

            PunchAtUtc: DateTime.SpecifyKind(
                timestamp, DateTimeKind.Utc)
        );
    }

    // ========================================
    // Row mapping: Attendance day
    // ========================================

    private static AttendanceDayDto MapDay(
        SqlDataReader r)
    {
        T? Nullable<T>(
            string column,
            Func<int, T> read)
            where T : struct
        {
            var index = r.GetOrdinal(column);

            return r.IsDBNull(index)
                ? null
                : read(index);
        }

        string? NullableString(string column)
        {
            var index = r.GetOrdinal(column);

            return r.IsDBNull(index)
                ? null
                : r.GetString(index);
        }

        return new AttendanceDayDto(
            AttendanceId: r.GetInt32(
                r.GetOrdinal("AttendanceId")),

            EmployeeId: r.GetInt32(
                r.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                r.GetDateTime(
                    r.GetOrdinal("WorkDate"))),

            LegacyStatus: r.GetString(
                r.GetOrdinal("LegacyStatus")),

            AssessmentStatus: NullableString(
                "AssessmentStatus"),

            IsScheduled: Nullable(
                "IsScheduled", r.GetBoolean),

            IsWeekend: Nullable(
                "IsWeekend", r.GetBoolean),

            IsHoliday: Nullable(
                "IsHoliday", r.GetBoolean),

            IsSpecialWorkDay: Nullable(
                "IsSpecialWorkDay", r.GetBoolean),

            ScheduledStartAt: Nullable(
                "ScheduledStartAt", r.GetDateTimeOffset),

            ScheduledEndAt: Nullable(
                "ScheduledEndAt", r.GetDateTimeOffset),

            RequiredMinutes: Nullable(
                "RequiredMinutes", r.GetInt32),

            WorkedMinutes: Nullable(
                "WorkedMinutes", r.GetInt32)
        );
    }

    // ========================================
    // POST: Record a punch
    // ========================================

    public async Task<AttendancePunchDto> RecordAsync(
        int employeeId,
        int actorUserId,
        RecordAttendancePunchDto dto,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_AttendancePunch_Record",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@WorkDate", dto.WorkDate);

                p.Add(
                    "@PunchType",
                    SqlDbType.VarChar,
                    3
                ).Value = dto.PunchType;

                AddInt(p, "@ActorUserId", actorUserId);
            },
            MapPunch,
            ct
        );

        return result ?? throw new InvalidOperationException(
            "Punch recording returned no result.");
    }

    // ========================================
    // GET: Employee punches for work date
    // ========================================

    public async Task<IReadOnlyList<AttendancePunchDto>> ListAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        return await executor.QueryAsync(
            "dbo.usp_AttendancePunch_List",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@WorkDate", workDate);
            },
            MapPunch,
            ct
        );
    }

    // ========================================
    // GET: Attendance day summary
    // ========================================

    public Task<AttendanceDayDto?> GetDayAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct)
    {
        return executor.QuerySingleAsync(
            "dbo.usp_AttendanceDay_Read",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@WorkDate", workDate);
            },
            MapDay,
            ct
        );
    }
}
