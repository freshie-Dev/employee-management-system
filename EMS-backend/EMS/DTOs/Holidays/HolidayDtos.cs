using System.ComponentModel.DataAnnotations;
namespace EMS.DTOs.Holidays;

public sealed record HolidayCalendarDto(int CalendarId, string Name, bool IsActive, int HolidayCount, bool IsDefault);
public sealed record HolidayDto(int HolidayId, int CalendarId, DateOnly HolidayDate, string Name);
public sealed record HolidayDateCheckDto(int CalendarId, DateOnly WorkDate, bool IsHoliday, int? HolidayId, string? HolidayName);

public sealed class SaveHolidayCalendarDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
}

public sealed class SetHolidayCalendarStatusDto
{
    public bool IsActive { get; init; }
}

public sealed class SaveHolidayDto
{
    [Required]
    public DateOnly? HolidayDate { get; init; }
    [Required, StringLength(150, MinimumLength = 1)]
    public required string Name { get; init; }
}
