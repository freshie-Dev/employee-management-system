
namespace EMS.Models;

public class UserAccount
{
    public int UserId { get; set; }

    public string Username { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optional: a user can exist without an employee.
    public Employee? Employee { get; set; }

    public ICollection<UserRole> UserRoles { get; set; }
        = new List<UserRole>();
}
