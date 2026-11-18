
using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.Attendance;

public sealed class RecordAttendancePunchDto
{
    public DateOnly WorkDate { get; init; }

    [Required]
    [RegularExpression("^(IN|OUT)$",
        ErrorMessage = "Punch type must be IN or OUT.")]
    public required string PunchType { get; init; }
}

public sealed record AttendancePunchDto(
    int PunchId,
    int AttendanceId,
    int EmployeeId,
    DateOnly WorkDate,
    string PunchType,
    DateTime PunchAtUtc
);

public sealed record AttendanceDayDto(
    int AttendanceId,
    int EmployeeId,
    DateOnly WorkDate,
    string LegacyStatus,
    string? AssessmentStatus,
    bool? IsScheduled,
    bool? IsWeekend,
    bool? IsHoliday,
    bool? IsSpecialWorkDay,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    int? RequiredMinutes,
    int? WorkedMinutes
);
