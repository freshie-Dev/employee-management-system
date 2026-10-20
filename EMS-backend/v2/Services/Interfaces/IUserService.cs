using EMS.DTOs.Users;
namespace EMS.Services.Interfaces;
public interface IUserService
{
    Task<List<UserResponseDto>> GetAllAsync(CancellationToken ct);
    Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
    Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct);
    Task<bool> UpdateStatusAsync(int id, bool enabled, CancellationToken ct);
    Task<bool> UpdateRolesAsync(int id, UpdateUserRolesDto dto, CancellationToken ct);
}
