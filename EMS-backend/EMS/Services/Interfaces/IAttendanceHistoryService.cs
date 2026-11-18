
using EMS.DTOs.Attendance;

namespace EMS.Services.Interfaces;

public interface IAttendanceHistoryService
{
    Task<AttendanceHistoryResponseDto> GetHistoryAsync(
        int employeeId,
        DateOnly fromDate,
        DateOnly toDate,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<AttendanceCorrectionResultDto> CorrectPunchAsync(
        int employeeId,
        DateOnly workDate,
        int actorUserId,
        CorrectAttendancePunchRequest request,
        CancellationToken ct);
}
