
using System.Data;
using EMS.DB.SPs;
using EMS.DTOs.Shifts;
using EMS.Services.Interfaces;
using Microsoft.Data.SqlClient;

namespace EMS.Services;

public sealed class ShiftService(
    IStoredProcedureExecutor executor
) : IShiftService
{
    // ========================================
    // Parameter helpers
    // ========================================

    private static void AddInt(
        SqlParameterCollection parameters,
        string name,
        int? value)
    {
        parameters.Add(name, SqlDbType.Int).Value =
            value.HasValue
                ? value.Value
                : DBNull.Value;
    }

    private static void AddTime(
        SqlParameterCollection parameters,
        string name,
        TimeOnly? value)
    {
        parameters.Add(name, SqlDbType.Time).Value =
            value.HasValue
                ? value.Value.ToTimeSpan()
                : DBNull.Value;
    }

    private static void AddName(
        SqlParameterCollection parameters,
        string name)
    {
        parameters.Add(
            "@Name",
            SqlDbType.NVarChar,
            100
        ).Value = name;
    }

    // ========================================
    // Result mapping
    // ========================================

    private static ShiftDto Map(SqlDataReader reader)
    {
        var configuredIndex = reader.GetOrdinal(
            "ConfiguredRequiredMinutes");

        return new ShiftDto(
            ShiftId: reader.GetInt32(
                reader.GetOrdinal("ShiftId")),

            Name: reader.GetString(
                reader.GetOrdinal("Name")),

            StartTime: TimeOnly.FromTimeSpan(
                reader.GetTimeSpan(
                    reader.GetOrdinal("StartTime"))),

            EndTime: TimeOnly.FromTimeSpan(
                reader.GetTimeSpan(
                    reader.GetOrdinal("EndTime"))),

            BreakMinutes: reader.GetInt32(
                reader.GetOrdinal("BreakMinutes")),

            IsActive: reader.GetBoolean(
                reader.GetOrdinal("IsActive")),

            GraceInMinutes: reader.GetInt32(
                reader.GetOrdinal("GraceInMinutes")),

            GraceOutMinutes: reader.GetInt32(
                reader.GetOrdinal("GraceOutMinutes")),

            ConfiguredRequiredMinutes:
                reader.IsDBNull(configuredIndex)
                    ? null
                    : reader.GetInt32(configuredIndex),

            EffectiveRequiredMinutes:
                reader.GetInt32(
                    reader.GetOrdinal("EffectiveRequiredMinutes")),

            IsOvernight: reader.GetBoolean(
                reader.GetOrdinal("IsOvernight"))
        );
    }

    // ========================================
    // GET: All shifts
    // ========================================

    public async Task<IReadOnlyList<ShiftDto>> GetAllAsync(
        bool includeInactive,
        CancellationToken ct)
    {
        return await executor.QueryAsync(
            "dbo.usp_Shift_List",
            parameters =>
            {
                parameters.Add(
                    "@IncludeInactive",
                    SqlDbType.Bit
                ).Value = includeInactive;
            },
            Map,
            ct);
    }

    // ========================================
    // GET: Shift by ID
    // ========================================

    public Task<ShiftDto?> GetByIdAsync(
        int id,
        CancellationToken ct)
    {
        return executor.QuerySingleAsync(
            "dbo.usp_Shift_GetById",
            parameters =>
            {
                AddInt(parameters, "@ShiftId", id);
            },
            Map,
            ct);
    }

    // ========================================
    // POST: Create shift
    // ========================================

    public Task<ShiftDto> CreateAsync(
        CreateShiftDto dto,
        CancellationToken ct)
    {
        return executor.ExecuteWithOutputAsync(
            "dbo.usp_Shift_Create",
            parameters =>
            {
                AddName(parameters, dto.Name.Trim());
                AddTime(parameters, "@StartTime", dto.StartTime);
                AddTime(parameters, "@EndTime", dto.EndTime);

                AddInt(
                    parameters, "@BreakMinutes",
                    dto.BreakMinutes);

                AddInt(
                    parameters, "@GraceInMinutes",
                    dto.GraceInMinutes);

                AddInt(
                    parameters, "@GraceOutMinutes",
                    dto.GraceOutMinutes);

                AddInt(
                    parameters, "@RequiredMinutes",
                    dto.RequiredMinutes);

                parameters.Add(
                    "@ShiftId",
                    SqlDbType.Int
                ).Direction = ParameterDirection.Output;
            },
            Map,
            (result, parameters) =>
            {
                if (result is not null)
                    return result;

                var id = parameters["@ShiftId"]?.Value;

                throw new InvalidOperationException(
                    $"Shift was created with ID {id}, " +
                    "but the procedure returned no shift details.");
            },
            ct);
    }

    // ========================================
    // PUT: Update shift
    // ========================================

    public async Task<ShiftDto> UpdateAsync(
        int id,
        UpdateShiftDto dto,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_Shift_Update",
            parameters =>
            {
                AddInt(parameters, "@ShiftId", id);
                AddName(parameters, dto.Name.Trim());

                AddTime(
                    parameters, "@StartTime",
                    dto.StartTime);

                AddTime(
                    parameters, "@EndTime",
                    dto.EndTime);

                AddInt(
                    parameters, "@BreakMinutes",
                    dto.BreakMinutes);
            },
            Map,
            ct);

        return result ?? throw new InvalidOperationException(
            "Shift update returned no result.");
    }

    // ========================================
    // PATCH: Shift status
    // ========================================

    public async Task<ShiftDto> SetStatusAsync(
        int id,
        bool isActive,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_Shift_SetStatus",
            parameters =>
            {
                AddInt(parameters, "@ShiftId", id);

                parameters.Add(
                    "@IsActive",
                    SqlDbType.Bit
                ).Value = isActive;
            },
            Map,
            ct);

        return result ?? throw new InvalidOperationException(
            "Status update returned no result.");
    }

    // ========================================
    // PUT: Attendance policy
    // ========================================

    public async Task<ShiftDto> UpdatePolicyAsync(
        int id,
        ShiftPolicyDto dto,
        CancellationToken ct)
    {
        var result = await executor.QuerySingleAsync(
            "dbo.usp_ShiftPolicy_Upsert",
            parameters =>
            {
                AddInt(parameters, "@ShiftId", id);

                AddInt(
                    parameters, "@GraceInMinutes",
                    dto.GraceInMinutes);

                AddInt(
                    parameters, "@GraceOutMinutes",
                    dto.GraceOutMinutes);

                AddInt(
                    parameters, "@RequiredMinutes",
                    dto.RequiredMinutes);
            },
            Map,
            ct);

        return result ?? throw new InvalidOperationException(
            "Policy update returned no result.");
    }
}
