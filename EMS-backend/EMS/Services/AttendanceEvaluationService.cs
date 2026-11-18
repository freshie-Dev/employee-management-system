
using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Attendance;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class AttendanceEvaluationService(
    IStoredProcedureExecutor executor
) : IAttendanceEvaluationService
{
    // ========================================
    // Parameter helpers
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
    // Nullable reader helpers
    // ========================================

    private static T? NullableValue<T>(
        SqlDataReader reader,
        string column,
        Func<int, T> getter)
        where T : struct
    {
        int index = reader.GetOrdinal(column);

        return reader.IsDBNull(index)
            ? null
            : getter(index);
    }

    private static string? NullableString(
        SqlDataReader reader,
        string column)
    {
        int index = reader.GetOrdinal(column);

        return reader.IsDBNull(index)
            ? null
            : reader.GetString(index);
    }

    private static DateTime AsUtc(DateTime value)
    {
        return DateTime.SpecifyKind(
            value,
            DateTimeKind.Utc);
    }

    // ========================================
    // Map attendance evaluation result
    // ========================================

    private static AttendanceEvaluationDto MapEvaluation(
        SqlDataReader r)
    {
        var firstIn = NullableValue(
            r,
            "FirstInAtUtc",
            r.GetDateTime
        );

        var lastOut = NullableValue(
            r,
            "LastOutAtUtc",
            r.GetDateTime
        );

        return new AttendanceEvaluationDto(
            AttendanceId: r.GetInt32(
                r.GetOrdinal("AttendanceId")),

            AssessmentId: r.GetInt64(
                r.GetOrdinal("AssessmentId")),

            EmployeeId: r.GetInt32(
                r.GetOrdinal("EmployeeId")),

            WorkDate: DateOnly.FromDateTime(
                r.GetDateTime(
                    r.GetOrdinal("WorkDate"))),

            AttendanceStatus: r.GetString(
                r.GetOrdinal("AttendanceStatus")),

            LegacyStatus: r.GetString(
                r.GetOrdinal("LegacyStatus")),

            IsFinalized: r.GetBoolean(
                r.GetOrdinal("IsFinalized")),

            IsScheduled: r.GetBoolean(
                r.GetOrdinal("IsScheduled")),

            IsWeekend: r.GetBoolean(
                r.GetOrdinal("IsWeekend")),

            IsHoliday: r.GetBoolean(
                r.GetOrdinal("IsHoliday")),

            IsSpecialWorkDay: r.GetBoolean(
                r.GetOrdinal("IsSpecialWorkDay")),

            ScheduledStartAt: NullableValue(
                r,
                "ScheduledStartAt",
                r.GetDateTimeOffset),

            ScheduledEndAt: NullableValue(
                r,
                "ScheduledEndAt",
                r.GetDateTimeOffset),

            RequiredMinutes: NullableValue(
                r,
                "RequiredMinutes",
                r.GetInt32),

            WorkedMinutes: NullableValue(
                r,
                "WorkedMinutes",
                r.GetInt32),

            PunchCount: r.GetInt32(
                r.GetOrdinal("PunchCount")),

            FirstInAtUtc: firstIn.HasValue
                ? AsUtc(firstIn.Value)
                : null,

            LastOutAtUtc: lastOut.HasValue
                ? AsUtc(lastOut.Value)
                : null,

            HasMissingCheckIn: r.GetBoolean(
                r.GetOrdinal("HasMissingCheckIn")),

            HasMissingCheckOut: r.GetBoolean(
                r.GetOrdinal("HasMissingCheckOut")),

            LateMinutes: r.GetInt32(
                r.GetOrdinal("LateMinutes")),

            EarlyOutMinutes: r.GetInt32(
                r.GetOrdinal("EarlyOutMinutes")),

            EvaluatedAtUtc: AsUtc(
                r.GetDateTime(
                    r.GetOrdinal("EvaluatedAt")))
        );
    }

    // ========================================
    // Map attendance exception
    // ========================================

    private static AttendanceExceptionDto MapException(
        SqlDataReader r)
    {
        return new AttendanceExceptionDto(
            ExceptionId: r.GetInt64(
                r.GetOrdinal("ExceptionId")),

            AssessmentId: r.GetInt64(
                r.GetOrdinal("AssessmentId")),

            ExceptionCode: r.GetString(
                r.GetOrdinal("ExceptionCode")),

            ExceptionName: r.GetString(
                r.GetOrdinal("ExceptionName")),

            Minutes: NullableValue(
                r,
                "Minutes",
                r.GetInt32)
        );
    }

    // ========================================
    // Evaluate attendance
    // ========================================

    public async Task<AttendanceEvaluationDto> EvaluateAsync(
        int employeeId,
        DateOnly workDate,
        bool finalize,
        int actorUserId,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_AttendanceAssessment_Evaluate",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);

                AddDate(p, "@WorkDate", workDate);

                p.Add(
                    "@Finalize",
                    SqlDbType.Bit
                ).Value = finalize;

                AddInt(
                    p,
                    "@ActorUserId",
                    actorUserId);
            },
            MapEvaluation,
            ct
        );

        return result ?? throw new InvalidOperationException(
            "Attendance evaluation returned no result.");
    }

    // ========================================
    // Get attendance exceptions
    // ========================================

    public async Task<IReadOnlyList<AttendanceExceptionDto>>
        GetExceptionsAsync(
            int employeeId,
            DateOnly workDate,
            CancellationToken ct)
    {
        return await executor.QueryAsync(
            "dbo.usp_AttendanceException_List",
            p =>
            {
                AddInt(p, "@EmployeeId", employeeId);
                AddDate(p, "@WorkDate", workDate);
            },
            MapException,
            ct
        );
    }
}
