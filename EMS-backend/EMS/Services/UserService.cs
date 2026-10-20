using EMS.DB;
using EMS.DTOs.Users;
using EMS.Models;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EMS.Services;
public sealed class UserService(AppDbContext db, IPasswordHasher<UserAccount> hasher) : IUserService
{
    private static UserResponseDto Map(UserAccount user) => new(user.UserId,
        user.EmployeeId, user.Username, user.IsActive, user.CreatedAt,
        user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToList());

    public async Task<List<UserResponseDto>> GetAllAsync(CancellationToken ct) =>
        (await db.UserAccounts.AsNoTracking().Include(x => x.UserRoles)
           .ThenInclude(x => x.Role).OrderBy(x => x.UserId).ToListAsync(ct))
        .Select(Map).ToList();

    public async Task<UserResponseDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var user = await db.UserAccounts.AsNoTracking().Include(x => x.UserRoles)
            .ThenInclude(x => x.Role).FirstOrDefaultAsync(x => x.UserId == id, ct);
        return user is null ? null : Map(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        var username = dto.Username.Trim();
        if (username.Length < 3) throw new ArgumentException("Invalid username.");
        if (await db.UserAccounts.AnyAsync(x => x.Username == username, ct))
            throw new InvalidOperationException("Username already exists.");
        var roles = dto.RoleIds.Distinct().ToList();
        if (await db.Roles.CountAsync(x => roles.Contains(x.RoleId), ct) != roles.Count)
            throw new ArgumentException("One or more role IDs do not exist.");
        var user = new UserAccount { Username = username, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        foreach (var roleId in roles) user.UserRoles.Add(new UserRole { RoleId = roleId });
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync(ct);
        return (await GetByIdAsync(user.UserId, ct))!;
    }

    public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct)
    {
        var user = await db.UserAccounts.FindAsync(new object[] { id }, ct);
        if (user is null) return null;
        var name = dto.Username.Trim();
        if (name.Length < 3) throw new ArgumentException("Invalid username.");
        if (await db.UserAccounts.AnyAsync(x => x.UserId != id && x.Username == name, ct))
            throw new InvalidOperationException("Username already exists.");
        user.Username = name;
        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<bool> UpdateStatusAsync(int id, bool enabled, CancellationToken ct)
    {
        var user = await db.UserAccounts.FindAsync(new object[] { id }, ct);
        if (user is null) return false;
        if (!enabled && await IsLastAdminAsync(id, ct))
            throw new InvalidOperationException("Cannot deactivate the last active administrator.");
        user.IsActive = enabled;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateRolesAsync(int id, UpdateUserRolesDto dto, CancellationToken ct)
    {
        var user = await db.UserAccounts.Include(x => x.UserRoles)
            .FirstOrDefaultAsync(x => x.UserId == id, ct);
        if (user is null) return false;
        var ids = dto.RoleIds.Distinct().ToList();
        if (await db.Roles.CountAsync(x => ids.Contains(x.RoleId), ct) != ids.Count)
            throw new ArgumentException("One or more role IDs do not exist.");
        var adminRoleId = await db.Roles.Where(x => x.Name == "SYSTEM_ADMIN")
            .Select(x => x.RoleId).SingleOrDefaultAsync(ct);
        if (user.UserRoles.Any(x => x.RoleId == adminRoleId) && !ids.Contains(adminRoleId)
            && await IsLastAdminAsync(id, ct))
            throw new InvalidOperationException("Cannot remove the last active administrator.");
        db.UserRoles.RemoveRange(user.UserRoles);
        foreach (var roleId in ids) db.UserRoles.Add(new UserRole { UserId = id, RoleId = roleId });
        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> IsLastAdminAsync(int userId, CancellationToken ct)
    {
        var isAdmin = await db.UserAccounts.AnyAsync(x => x.UserId == userId && x.IsActive &&
            x.UserRoles.Any(ur => ur.Role.Name == "SYSTEM_ADMIN"), ct);
        if (!isAdmin) return false;
        return !await db.UserAccounts.AnyAsync(x => x.UserId != userId && x.IsActive &&
            x.UserRoles.Any(ur => ur.Role.Name == "SYSTEM_ADMIN"), ct);
    }
}
