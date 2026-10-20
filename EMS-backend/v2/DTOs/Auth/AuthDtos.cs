using System.ComponentModel.DataAnnotations;
namespace EMS.DTOs.Auth;
public sealed class LoginDto
{
    [Required] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public sealed record LoginResponseDto(string AccessToken, string TokenType, DateTime ExpiresAt,
    int UserId, string Username, List<string> Roles);
