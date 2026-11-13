
using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.ScheduleOverrides;

public sealed record ScheduleOverrideDto(
    long OverrideId,
    int EmployeeId,
    DateOnly WorkDate,
    string OverrideType,
    DateTimeOffset? StartAt,
    DateTimeOffset? EndAt,
    int BreakMinutes,
    int GraceInMinutes,
    int GraceOutMinutes,
    int? RequiredMinutes,
    string? Reason,
    int AssignedByUserId,
    string RecordStatus,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion
);

public sealed class CreateScheduleOverrideDto
{
    public DateOnly WorkDate { get; init; }

    [Required]
    public required string OverrideType { get; init; }

    public DateTimeOffset? StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }

    [Range(0, 1440)]
    public int BreakMinutes { get; init; }

    [Range(0, 240)]
    public int GraceInMinutes { get; init; }

    [Range(0, 240)]
    public int GraceOutMinutes { get; init; }

    [Range(0, 1440)]
    public int? RequiredMinutes { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed class UpdateScheduleOverrideDto
{
    [Required]
    public required string RowVersion { get; init; }

    [Required]
    public required string OverrideType { get; init; }

    public DateTimeOffset? StartAt { get; init; }
    public DateTimeOffset? EndAt { get; init; }

    [Range(0, 1440)]
    public int BreakMinutes { get; init; }

    [Range(0, 240)]
    public int GraceInMinutes { get; init; }

    [Range(0, 240)]
    public int GraceOutMinutes { get; init; }

    [Range(0, 1440)]
    public int? RequiredMinutes { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed class CancelScheduleOverrideDto
{
    [Required]
    public required string RowVersion { get; init; }
}

public sealed record ResolvedEmployeeScheduleDto(
    int EmployeeId,
    DateOnly WorkDate,
    string ScheduleSource,
    long? OverrideId,
    int? ShiftId,
    string? OverrideType,
    bool IsScheduled,
    DateTimeOffset? ScheduledStartAt,
    DateTimeOffset? ScheduledEndAt,
    int? BreakMinutes,
    int? GraceInMinutes,
    int? GraceOutMinutes,
    int? RequiredMinutes
);
