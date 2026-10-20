using EMS.DTOs.Auth;
namespace EMS.Services.Interfaces;
public interface IAuthService
{
    Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct);
}
