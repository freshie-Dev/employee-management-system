using EMS.DTOs.Shifts;

namespace EMS.Services.Interfaces;

public interface IShiftService
{
    Task<IReadOnlyList<ShiftDto>> GetAllAsync(
        bool includeInactive,
        CancellationToken ct);

    Task<ShiftDto?> GetByIdAsync(
        int id,
        CancellationToken ct);

    Task<ShiftDto> CreateAsync(
        CreateShiftDto dto,
        CancellationToken ct);

    Task<ShiftDto> UpdateAsync(
        int id,
        UpdateShiftDto dto,
        CancellationToken ct);

    Task<ShiftDto> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken ct);

    Task<ShiftDto> UpdatePolicyAsync(
        int id,
        ShiftPolicyDto dto,
        CancellationToken ct);
}