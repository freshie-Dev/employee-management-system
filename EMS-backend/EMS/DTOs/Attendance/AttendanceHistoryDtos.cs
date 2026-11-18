
using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.Attendance;

public sealed record AttendanceHistoryItemDto(
    int AttendanceId,
    int EmployeeId,
    DateOnly WorkDate,
    string AttendanceStatus,
    string LegacyStatus,
    bool IsFinalized,
    bool? IsScheduled,
    bool? IsWeekend,
    bool? IsHoliday,
    bool? IsSpecialWorkDay,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    int? RequiredMinutes,
    int? WorkedMinutes,
    int PunchCount,
    DateTime? FirstInAtUtc,
    DateTime? LastOutAtUtc,
    int ExceptionCount,
    long TotalCount
);

public sealed record AttendanceHistoryResponseDto(
    IReadOnlyList<AttendanceHistoryItemDto> Items,
    long TotalCount,
    int Page,
    int PageSize
);

public sealed class CorrectAttendancePunchRequest
{
    [Required]
    [RegularExpression("^(INSERT|UPDATE|DELETE)$")]
    public required string Action { get; init; }

    public int? PunchId { get; init; }

    [RegularExpression("^(IN|OUT)$")]
    public string? PunchType { get; init; }

    public DateTimeOffset? PunchAtUtc { get; init; }

    [RegularExpression("^(IN|OUT)$")]
    public string? ExpectedPunchType { get; init; }

    public DateTimeOffset? ExpectedPunchAtUtc { get; init; }

    [Required]
    [MinLength(5)]
    [MaxLength(500)]
    public required string Reason { get; init; }
}

public sealed record AttendanceCorrectionResultDto(
    int PunchId,
    int AttendanceId,
    int EmployeeId,
    DateOnly WorkDate,
    string CorrectionAction,
    string? PunchType,
    DateTime? PunchAtUtc,
    string AttendanceStatus,
    string Reason
);
