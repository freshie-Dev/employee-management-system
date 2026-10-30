using System.ComponentModel.DataAnnotations;

namespace EMS.DTOs.Shifts;

public sealed record ShiftDto(
    int ShiftId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int BreakMinutes,
    bool IsActive,
    int GraceInMinutes,
    int GraceOutMinutes,
    int? ConfiguredRequiredMinutes,
    int EffectiveRequiredMinutes,
    bool IsOvernight
);

public sealed class CreateShiftDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    [Range(0, 1439)]
    public int BreakMinutes { get; init; }

    [Range(0, 240)]
    public int GraceInMinutes { get; init; }

    [Range(0, 240)]
    public int GraceOutMinutes { get; init; }

    [Range(0, 1439)]
    public int? RequiredMinutes { get; init; }
}

public sealed class UpdateShiftDto
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    public TimeOnly? StartTime { get; init; }

    public TimeOnly? EndTime { get; init; }

    [Range(0, 1439)]
    public int BreakMinutes { get; init; }
}

public sealed class UpdateShiftStatusDto
{
    public bool IsActive { get; init; }
}

public sealed class ShiftPolicyDto
{
    [Range(0, 240)]
    public int GraceInMinutes { get; init; }

    [Range(0, 240)]
    public int GraceOutMinutes { get; init; }

    [Range(0, 1439)]
    public int? RequiredMinutes { get; init; }
}