using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.EmployeeShifts;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class EmployeeShiftService(IStoredProcedureExecutor executor) : IEmployeeShiftService
{
    private static EmployeeShiftAssignmentDto Map(SqlDataReader r)
    {
        int ix = r.GetOrdinal("EffectiveTo");
        return new EmployeeShiftAssignmentDto(
            r.GetInt32(r.GetOrdinal("AssignmentId")),
            r.GetInt32(r.GetOrdinal("EmployeeId")),
            r.GetInt32(r.GetOrdinal("ShiftId")),
            r.GetString(r.GetOrdinal("ShiftName")),
            TimeOnly.FromTimeSpan(r.GetTimeSpan(r.GetOrdinal("StartTime"))),
            TimeOnly.FromTimeSpan(r.GetTimeSpan(r.GetOrdinal("EndTime"))),
            r.GetInt32(r.GetOrdinal("BreakMinutes")),
            DateOnly.FromDateTime(r.GetDateTime(r.GetOrdinal("EffectiveFrom"))),
            r.IsDBNull(ix) ? null : DateOnly.FromDateTime(r.GetDateTime(ix)));
    }

    public async Task<IReadOnlyList<EmployeeShiftAssignmentDto>> ListAsync(int employeeId, CancellationToken ct) =>
        await executor.QueryAsync("dbo.usp_EmployeeShift_List",
            p => p.Add("@EmployeeId", SqlDbType.Int).Value = employeeId,
            Map, ct);

    public Task<EmployeeShiftAssignmentDto?> GetEffectiveAsync(int employeeId, DateOnly workDate, CancellationToken ct) =>
        executor.QuerySingleAsync("dbo.usp_EmployeeShift_GetEffective", p =>
        {
            p.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
            p.Add("@WorkDate", SqlDbType.Date).Value = workDate.ToDateTime(TimeOnly.MinValue);
        }, Map, ct);

    public async Task<EmployeeShiftAssignmentDto> AssignAsync(int employeeId, AssignEmployeeShiftDto dto, CancellationToken ct)
    {
        if (dto.EffectiveFrom == default)
            throw new ArgumentException("EffectiveFrom is required.");
        var row = await executor.QuerySingleAsync("dbo.usp_EmployeeShift_Assign", p =>
        {
            p.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
            p.Add("@ShiftId", SqlDbType.Int).Value = dto.ShiftId;
            p.Add("@EffectiveFrom", SqlDbType.Date).Value = dto.EffectiveFrom.ToDateTime(TimeOnly.MinValue);
        }, Map, ct);
        return row ?? throw new InvalidOperationException("Shift assignment procedure returned no row.");
    }

    public async Task<IReadOnlyList<EmployeeShiftAssignmentDto>> InitializeDefaultAsync(int employeeId, CancellationToken ct) =>
        await executor.QueryAsync("dbo.usp_EmployeeShift_InitializeDefault",
            p => p.Add("@EmployeeId", SqlDbType.Int).Value = employeeId,
            Map, ct);
}
