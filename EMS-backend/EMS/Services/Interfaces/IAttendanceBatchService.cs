using EMS.DTOs.Attendance;

namespace EMS.Services.Interfaces;

public interface IAttendanceBatchService
{
    Task<AttendanceBatchResultDto> RunAsync(
        DateOnly workDate,
        bool dryRun,
        int actorUserId,
        int maxEmployees,
        CancellationToken cancellationToken);
}
