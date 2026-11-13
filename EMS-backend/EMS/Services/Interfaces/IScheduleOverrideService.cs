
using EMS.DTOs.ScheduleOverrides;

namespace EMS.Services.Interfaces;

public interface IScheduleOverrideService
{
    Task<IReadOnlyList<ScheduleOverrideDto>> ListAsync(
        int employeeId,
        DateOnly? from,
        DateOnly? to,
        bool includeCancelled,
        CancellationToken ct);

    Task<ScheduleOverrideDto?> GetAsync(
        int employeeId,
        long overrideId,
        CancellationToken ct);

    Task<ScheduleOverrideDto> CreateAsync(
        int employeeId,
        int actorUserId,
        CreateScheduleOverrideDto dto,
        CancellationToken ct);

    Task<ScheduleOverrideDto> UpdateAsync(
        int employeeId,
        long overrideId,
        UpdateScheduleOverrideDto dto,
        CancellationToken ct);

    Task<ScheduleOverrideDto> CancelAsync(
        int employeeId,
        long overrideId,
        CancelScheduleOverrideDto dto,
        CancellationToken ct);

    Task<ResolvedEmployeeScheduleDto> ResolveAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct);
}
