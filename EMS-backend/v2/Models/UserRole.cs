namespace EMS.Models;
public class UserRole
{
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public UserAccount User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
