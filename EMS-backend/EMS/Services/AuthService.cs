using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EMS.DB;
using EMS.DTOs.Auth;
using EMS.Models;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EMS.Services;
public sealed class AuthService(AppDbContext db, IPasswordHasher<UserAccount> hasher,
    IConfiguration config) : IAuthService
{
    public async Task<LoginResponseDto?> LoginAsync(LoginDto dto, CancellationToken ct)
    {
        var user = await db.UserAccounts.Include(x => x.UserRoles).ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Username == dto.Username.Trim(), ct);
        if (user is null) return null;
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed || !user.IsActive) return null;
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        var roles = user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToList();
        var expiry = DateTime.UtcNow.AddMinutes(15);
        var claims = new List<Claim> {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var signingKey = config["Jwt:Key"]!;
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"],
            claims, expires: expiry, signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                SecurityAlgorithms.HmacSha256));
        return new LoginResponseDto(new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer", expiry, user.UserId, user.Username, roles);
    }
}
