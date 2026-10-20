using System.ComponentModel.DataAnnotations;
namespace EMS.DTOs.Users;
public sealed class CreateUserDto
{
    [Required, StringLength(100, MinimumLength = 3)] public string Username { get; set; } = "";
    [Required, MinLength(12)] public string Password { get; set; } = "";
    public List<int> RoleIds { get; set; } = new();
}
public sealed class UpdateUserDto
{
    [Required, StringLength(100, MinimumLength = 3)] public string Username { get; set; } = "";
}
public sealed class UpdateUserStatusDto { public bool IsActive { get; set; } }
public sealed class UpdateUserRolesDto
{
    [Required] public List<int> RoleIds { get; set; } = new();
}
public sealed record UserResponseDto(int UserId, int? EmployeeId, string Username,
    bool IsActive, DateTime CreatedAt, List<string> Roles);
