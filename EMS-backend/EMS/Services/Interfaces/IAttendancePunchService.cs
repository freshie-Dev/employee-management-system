
using EMS.DTOs.Attendance;

namespace EMS.Services.Interfaces;

public interface IAttendancePunchService
{
    Task<AttendancePunchDto> RecordAsync(
        int employeeId,
        int actorUserId,
        RecordAttendancePunchDto dto,
        CancellationToken ct);

    Task<IReadOnlyList<AttendancePunchDto>> ListAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct);

    Task<AttendanceDayDto?> GetDayAsync(
        int employeeId,
        DateOnly workDate,
        CancellationToken ct);
}
