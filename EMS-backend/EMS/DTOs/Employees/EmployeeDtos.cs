using System.ComponentModel.DataAnnotations;
namespace EMS.DTOs.Employees;
public class CreateEmployeeDto {
    public int? UserId {get;set;}
    [Required, StringLength(30)] public string EmployeeCode {get;set;}="";
    [Required, StringLength(100)] public string FirstName {get;set;}="";
    [Required, StringLength(100)] public string LastName {get;set;}="";
    [Required, EmailAddress, StringLength(255)] public string WorkEmail {get;set;}="";
    [EmailAddress, StringLength(255)] public string? PersonalEmail {get;set;}
    [StringLength(30)] public string? PhoneNumber {get;set;}
    public DateTime? DateOfBirth {get;set;}
    [Required] public DateTime HireDate {get;set;}
    [Required] public string EmploymentType {get;set;}="";
}
public class UpdateEmployeeDto {
    [Required, StringLength(100)] public string FirstName {get;set;}="";
    [Required, StringLength(100)] public string LastName {get;set;}="";
    [Required, EmailAddress, StringLength(255)] public string WorkEmail {get;set;}="";
    [EmailAddress, StringLength(255)] public string? PersonalEmail {get;set;}
    [StringLength(30)] public string? PhoneNumber {get;set;}
    public DateTime? DateOfBirth {get;set;}
    [Required] public string EmploymentType {get;set;}="";
}

public record UpdateAssignmentDto(
    int DepartmentId,
    int DesignationId,
    int? ManagerId,
    DateTime EffectiveFrom
);
public record ChangeEmploymentStatusDto([Required] string Status);
public record LinkUserDto(int? UserId);
public record CreateAssignmentDto(int DepartmentId, int DesignationId, int? ManagerId, DateTime EffectiveFrom);
public record EmployeeDto(int EmployeeId, int? UserId, string EmployeeCode, string FirstName, string LastName, string WorkEmail, string EmploymentType, string EmploymentStatus, DateTime HireDate);
public record AssignmentDto(int AssignmentId, int EmployeeId, int DepartmentId, int DesignationId, int? ManagerId, DateTime EffectiveFrom, DateTime? EffectiveTo);
