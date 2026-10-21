using System.ComponentModel.DataAnnotations;
namespace EMS.DTOs.Organization;
public record CreateDepartmentDto([Required, StringLength(100)] string Name, [Required, StringLength(20)] string Code, int? ParentDepartmentId);
public record UpdateDepartmentDto([Required, StringLength(100)] string Name, [Required, StringLength(20)] string Code, int? ParentDepartmentId);
public record DepartmentDto(int DepartmentId, string Name, string Code, int? ParentDepartmentId, bool IsActive);
public record CreateDesignationDto([Required, StringLength(100)] string Title, [Required, StringLength(20)] string Code, [StringLength(500)] string? Description);
public record UpdateDesignationDto([Required, StringLength(100)] string Title, [Required, StringLength(20)] string Code, [StringLength(500)] string? Description);
public record DesignationDto(int DesignationId, string Title, string Code, string? Description, bool IsActive);
public record SetStatusDto(bool IsActive);
