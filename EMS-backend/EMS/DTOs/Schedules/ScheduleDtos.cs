using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.Schedules;

public sealed record ScheduleSettingsDto(
    byte SettingsId,
    int? DefaultShiftId,
    string? DefaultShiftName,
    int? DefaultScheduleId,
    string? DefaultScheduleName,
    int? DefaultHolidayCalendarId,
    DateTime UpdatedAt);

public sealed class UpdateScheduleSettingsDto
{
    [Range(1, int.MaxValue)] public int DefaultShiftId { get; init; }
    [Range(1, int.MaxValue)] public int DefaultScheduleId { get; init; }
    [Range(1, int.MaxValue)] public int? DefaultHolidayCalendarId { get; init; }
}

public sealed record WorkScheduleDayDto(byte DayOfWeek, bool IsWorkingDay);

public sealed record WorkScheduleDto(
    int ScheduleId,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<WorkScheduleDayDto> Days);

public sealed class CreateWorkScheduleDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required] public required List<byte> WorkingDays { get; init; }
}
