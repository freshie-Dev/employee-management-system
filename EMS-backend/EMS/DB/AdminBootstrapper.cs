using EMS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace EMS.DB;
public static class AdminBootstrapper
{
    public static async Task RunAsync(IServiceProvider services, string username, string password)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<UserAccount>>();
        var role = await db.Roles.SingleOrDefaultAsync(x => x.Name == "SYSTEM_ADMIN")
            ?? throw new InvalidOperationException("SYSTEM_ADMIN role not found. Run the EMS schema seed first.");
        if (await db.UserAccounts.AnyAsync(x => x.UserRoles.Any(ur => ur.RoleId == role.RoleId)))
            throw new InvalidOperationException("An administrator already exists.");
        if (await db.UserAccounts.AnyAsync(x => x.Username == username))
            throw new InvalidOperationException("Username already exists.");
        var user = new UserAccount { Username = username, CreatedAt = DateTime.UtcNow };
        user.PasswordHash = hasher.HashPassword(user, password);
        user.UserRoles.Add(new UserRole { RoleId = role.RoleId });
        db.UserAccounts.Add(user);
        await db.SaveChangesAsync();
    }
}
