
using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class AttendanceHistoryService(
    IStoredProcedureExecutor executor
) : IAttendanceHistoryService
{
    // ==========================================
    // SQL helpers
    // ==========================================

    private static void AddInt(
        SqlParameterCollection parameters,
        string name,
        int value)
    {
        parameters.Add(name, SqlDbType.Int).Value = value;
    }

    private static void AddDate(
        SqlParameterCollection parameters,
        string name,
        DateOnly value)
    {
        parameters.Add(name, SqlDbType.Date).Value =
            value.ToDateTime(TimeOnly.MinValue);
    }

    private static T? Nullable<T>(
        SqlDataReader reader,
        string column,
        Func<int, T> getter)
        where T : struct
    {
        var index = reader.GetOrdinal(column);

        return reader.IsDBNull(index)
            ? null
            : getter(index);
    }

    private static string? NullableString(
        SqlDataReader reader,
        string column)
    {
        var index = reader.GetOrdinal(column);

        return reader.IsDBNull(index)
            ? null
            : reader.GetString(index);
    }

    private static DateTime? UtcDate(
        SqlDataReader reader,
        string column)
    {
        var value = Nullable(
            reader,
            column,
            reader.GetDateTime);

        return value.HasValue
            ? DateTime.SpecifyKind(
                value.Value,
                DateTimeKind.Utc)
            : null;
    }

    // ==========================================
    // Map attendance history row
    // ==========================================

    private static AttendanceHistoryItemDto MapHistory(
        SqlDataReader r)
    {
        return new AttendanceHistoryItemDto(
            AttendanceId: r.GetInt32(
                r.GetOrdinal("AttendanceId")),

            EmployeeId: r.GetInt32(
                r.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                r.GetDateTime(r.GetOrdinal("WorkDate"))),

            AttendanceStatus: r.GetString(
                r.GetOrdinal("AttendanceStatus")),

            LegacyStatus: r.GetString(
                r.GetOrdinal("LegacyStatus")),

            IsFinalized: r.GetBoolean(
                r.GetOrdinal("IsFinalized")),

            IsScheduled: Nullable(
                r, "IsScheduled", r.GetBoolean),

            IsWeekend: Nullable(
                r, "IsWeekend", r.GetBoolean),

            IsHoliday: Nullable(
                r, "IsHoliday", r.GetBoolean),

            IsSpecialWorkDay: Nullable(
                r, "IsSpecialWorkDay", r.GetBoolean),

            ScheduledStartAt: Nullable(
                r, "ScheduledStartAt", r.GetDateTimeOffset),

            ScheduledEndAt: Nullable(
                r, "ScheduledEndAt", r.GetDateTimeOffset),

            RequiredMinutes: Nullable(
                r, "RequiredMinutes", r.GetInt32),

            WorkedMinutes: Nullable(
                r, "WorkedMinutes", r.GetInt32),

            PunchCount: r.GetInt32(
                r.GetOrdinal("PunchCount")),

            FirstInAtUtc: UtcDate(
                r, "FirstInAtUtc"),

            LastOutAtUtc: UtcDate(
                r, "LastOutAtUtc"),

            ExceptionCount: r.GetInt32(
                r.GetOrdinal("ExceptionCount")),

            TotalCount: r.GetInt64(
                r.GetOrdinal("TotalCount"))
        );
    }

    // ==========================================
    // Map correction result
    // ==========================================

    private static AttendanceCorrectionResultDto MapCorrection(
        SqlDataReader r)
    {
        return new AttendanceCorrectionResultDto(
            PunchId: r.GetInt32(
                r.GetOrdinal("PunchId")),

            AttendanceId: r.GetInt32(
                r.GetOrdinal("AttendanceId")),

            EmployeeId: r.GetInt32(
                r.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                r.GetDateTime(r.GetOrdinal("WorkDate"))),

            CorrectionAction: r.GetString(
                r.GetOrdinal("CorrectionAction")),

            PunchType: NullableString(
                r, "PunchType"),

            PunchAtUtc: UtcDate(
                r, "PunchAtUtc"),

            AttendanceStatus: r.GetString(
                r.GetOrdinal("AttendanceStatus")),

            Reason: r.GetString(
                r.GetOrdinal("Reason"))
        );
    }

    // ==========================================
    // Get attendance history
    // ==========================================

    public async Task<AttendanceHistoryResponseDto> GetHistoryAsync(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        var items = await executor.QueryAsync(
            "dbo.usp_AttendanceHistory_List",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@FromDate", fromDate);
                AddDate(p, "@ToDate", toDate);
                AddInt(p, "@Page", page);
                AddInt(p, "@PageSize", pageSize);
            },
            MapHistory,
            ct
        );

        var totalCount = items.Count > 0
            ? items[0].TotalCount
            : 0;

        return new AttendanceHistoryResponseDto(
            items,
            totalCount,
            page,
            pageSize
        );
    }

    // ==========================================
    // Correct attendance punch
    // ==========================================

    public async Task<AttendanceCorrectionResultDto> CorrectPunchAsync(
        int employeeId,
        DateOnly workDate,
        int actorUserId,
        CorrectAttendancePunchRequest request,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_AttendancePunch_Correct",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@WorkDate", workDate);

                p.Add(
                    "@Action",
                    SqlDbType.VarChar,
                    6
                ).Value = request.Action;

                p.Add(
                    "@PunchId",
                    SqlDbType.Int
                ).Value =
                    (object?)request.PunchId ?? DBNull.Value;

                p.Add(
                    "@PunchType",
                    SqlDbType.VarChar,
                    3
                ).Value =
                    (object?)request.PunchType ?? DBNull.Value;

                p.Add(
                    "@PunchAtUtc",
                    SqlDbType.DateTime2
                ).Value =
                    (object?)request.PunchAtUtc?.UtcDateTime
                    ?? DBNull.Value;

                p.Add(
                    "@ExpectedPunchType",
                    SqlDbType.VarChar,
                    3
                ).Value =
                    (object?)request.ExpectedPunchType
                    ?? DBNull.Value;

                p.Add(
                    "@ExpectedPunchAtUtc",
                    SqlDbType.DateTime2
                ).Value =
                    (object?)request.ExpectedPunchAtUtc?.UtcDateTime
                    ?? DBNull.Value;

                p.Add(
                    "@Reason",
                    SqlDbType.NVarChar,
                    500
                ).Value = request.Reason.Trim();

                AddInt(p, "@ActorUserId", actorUserId);
            },
            MapCorrection,
            ct
        );

        return result ?? throw new InvalidOperationException(
            "Attendance correction returned no result.");
    }
}
