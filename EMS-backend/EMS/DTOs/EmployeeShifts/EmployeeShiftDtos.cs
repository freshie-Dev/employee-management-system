using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.EmployeeShifts;

public sealed record EmployeeShiftAssignmentDto(
    int AssignmentId, int EmployeeId, int ShiftId, string ShiftName,
    TimeOnly StartTime, TimeOnly EndTime, int BreakMinutes,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo);

public sealed class AssignEmployeeShiftDto
{
    [Range(1, int.MaxValue)] public int ShiftId { get; init; }
    public DateOnly EffectiveFrom { get; init; }
}
