using EMS.DTOs.EmployeeShifts;

namespace EMS.Services.Interfaces;

public interface IEmployeeShiftService
{
    Task<IReadOnlyList<EmployeeShiftAssignmentDto>> ListAsync(int employeeId, CancellationToken ct);
    Task<EmployeeShiftAssignmentDto?> GetEffectiveAsync(int employeeId, DateOnly workDate, CancellationToken ct);
    Task<EmployeeShiftAssignmentDto> AssignAsync(int employeeId, AssignEmployeeShiftDto dto, CancellationToken ct);
    Task<IReadOnlyList<EmployeeShiftAssignmentDto>> InitializeDefaultAsync(int employeeId, CancellationToken ct);
}
