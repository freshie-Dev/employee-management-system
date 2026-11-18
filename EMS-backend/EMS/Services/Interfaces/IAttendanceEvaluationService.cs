
using EMS.DTOs.Attendance;

namespace EMS.Services.Interfaces;

public interface IAttendanceEvaluationService
{
    Task<AttendanceEvaluationDto> EvaluateAsync(
        int employeeId,
        DateOnly workDate,
        bool finalize,
        int actorUserId,
        CancellationToken ct);

    Task<IReadOnlyList<AttendanceExceptionDto>> GetExceptionsAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct);
}
