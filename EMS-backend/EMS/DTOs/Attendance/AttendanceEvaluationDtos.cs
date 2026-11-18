
using System.Text.Json.Serialization;

namespace EMS.DTOs.Attendance;

public sealed class EvaluateAttendanceRequest
{
    [JsonPropertyName("finalize")]
    public bool FinalizeAttendance { get; init; }
}

public sealed record AttendanceEvaluationDto(
    int AttendanceId,
    long AssessmentId,
    int EmployeeId,
    DateOnly WorkDate,
    string AttendanceStatus,
    string LegacyStatus,
    bool IsFinalized,
    bool IsScheduled,
    bool IsWeekend,
    bool IsHoliday,
    bool IsSpecialWorkDay,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    int? RequiredMinutes,
    int? WorkedMinutes,
    int PunchCount,
    DateTime? FirstInAtUtc,
    DateTime? LastOutAtUtc,
    bool HasMissingCheckIn,
    bool HasMissingCheckOut,
    int LateMinutes,
    int EarlyOutMinutes,
    DateTime EvaluatedAtUtc
);

public sealed record AttendanceExceptionDto(
    long ExceptionId,
    long AssessmentId,
    string ExceptionCode,
    string ExceptionName,
    int? Minutes
);
