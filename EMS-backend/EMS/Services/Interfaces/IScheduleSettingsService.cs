using EMS.DTOs.Schedules;

namespace EMS.Services.Interfaces;

public interface IScheduleSettingsService
{
    Task<ScheduleSettingsDto?> GetAsync(CancellationToken ct);
    Task<ScheduleSettingsDto> UpdateAsync(UpdateScheduleSettingsDto dto, CancellationToken ct);
    Task<IReadOnlyList<WorkScheduleDto>> ListWorkSchedulesAsync(bool includeInactive, CancellationToken ct);
    Task<WorkScheduleDto> CreateWorkScheduleAsync(CreateWorkScheduleDto dto, CancellationToken ct);
}
