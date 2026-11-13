using EMS.DTOs.Holidays;
namespace EMS.Services.Interfaces;

public interface IHolidayService
{
    Task<IReadOnlyList<HolidayCalendarDto>> ListCalendarsAsync(bool includeInactive, CancellationToken ct);
    Task<HolidayCalendarDto?> GetCalendarAsync(int id, CancellationToken ct);
    Task<HolidayCalendarDto> CreateCalendarAsync(SaveHolidayCalendarDto dto, CancellationToken ct);
    Task<HolidayCalendarDto> UpdateCalendarAsync(int id, SaveHolidayCalendarDto dto, CancellationToken ct);
    Task<HolidayCalendarDto> SetCalendarStatusAsync(int id, bool isActive, CancellationToken ct);
    Task<IReadOnlyList<HolidayDto>> ListHolidaysAsync(int calendarId, DateOnly? from, DateOnly? to, CancellationToken ct);
    Task<HolidayDto?> GetHolidayAsync(int calendarId, int holidayId, CancellationToken ct);
    Task<HolidayDto> CreateHolidayAsync(int calendarId, SaveHolidayDto dto, CancellationToken ct);
    Task<HolidayDto> UpdateHolidayAsync(int calendarId, int holidayId, SaveHolidayDto dto, CancellationToken ct);
    Task DeleteHolidayAsync(int calendarId, int holidayId, CancellationToken ct);
    Task<HolidayDateCheckDto> CheckDateAsync(int calendarId, DateOnly workDate, CancellationToken ct);
}
