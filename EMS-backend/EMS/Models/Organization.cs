
namespace EMS.Models;

public class Department
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int? ParentDepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Designation
{
    public int DesignationId { get; set; }
    public string Title { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Employee
{
    public int EmployeeId { get; set; }

    // Required foreign key.
    public int UserId { get; set; }

    public UserAccount User { get; set; } = null!;

    public string EmployeeCode { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string WorkEmail { get; set; } = "";
    public string? PersonalEmail { get; set; }
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public DateTime HireDate { get; set; }

    public string EmploymentType { get; set; } = "";
    public string EmploymentStatus { get; set; } = "ACTIVE";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class EmployeeAssignment
{
    public int AssignmentId { get; set; }
    public int EmployeeId { get; set; }
    public int DepartmentId { get; set; }
    public int DesignationId { get; set; }
    public int? ManagerId { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
