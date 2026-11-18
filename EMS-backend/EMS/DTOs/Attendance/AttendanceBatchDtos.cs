using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.Attendance;

public sealed class RunAttendanceBatchRequest
{
    public DateOnly WorkDate { get; init; }

    // Safe default: show potential actions without writing to the database.
    public bool DryRun { get; init; } = true;

    [Range(1, 5000)]
    public int MaxEmployees { get; init; } = 500;
}

public sealed record AttendanceBatchCandidateDto(
    int EmployeeId,
    DateOnly WorkDate,
    string Decision,
    string ReasonCode,
    DateTimeOffset? ScheduledEndAt,
    int? ExistingAttendanceId,
    int PunchCount
);

public sealed record AttendanceBatchEmployeeResultDto(
    int EmployeeId,
    string Status,
    string ReasonCode,
    string? AttendanceStatus = null,
    string? Error = null
);

public sealed record AttendanceBatchResultDto(
    DateOnly WorkDate,
    bool DryRun,
    int Scanned,
    int Eligible,
    int Processed,
    int Skipped,
    int Failed,
    bool LimitReached,
    IReadOnlyList<AttendanceBatchEmployeeResultDto> Results
);
