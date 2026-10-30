
using EMS.DB;
using EMS.DTOs.Employees;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EMS.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Roles = "SYSTEM_ADMIN,HR_MANAGER")]
public class EmployeesController(AppDbContext db) : ControllerBase
{
    // ==========================================
    // Helpers
    // ==========================================

    private static EmployeeDto Map(Employee e) =>
        new(
            e.EmployeeId,
            e.UserId,
            e.EmployeeCode,
            e.FirstName,
            e.LastName,
            e.WorkEmail,
            e.EmploymentType,
            e.EmploymentStatus,
            e.HireDate
        );

    private static bool ValidType(string value) =>
        value is "PERMANENT" or "CONTRACT" or "INTERN";

    // ==========================================
    // List employees
    // ==========================================

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest(new
            {
                message = "Invalid pagination."
            });

        var query = db.Employees
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();

            query = query.Where(e =>
                e.FirstName.Contains(term)
                || e.LastName.Contains(term)
                || e.EmployeeCode.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(e => e.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return Ok(new
        {
            total,
            page,
            pageSize,
            items = rows.Select(Map)
        });
    }

    // ==========================================
    // Get employee
    // ==========================================

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(
        int id,
        CancellationToken ct)
    {
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.EmployeeId == id, ct);

        return employee is null
            ? NotFound()
            : Ok(Map(employee));
    }

    // ==========================================
    // Available user accounts
    // ==========================================

    [HttpGet("available-users")]
    public async Task<IActionResult> AvailableUsers(
        CancellationToken ct)
    {
        var users = await db.UserAccounts
            .AsNoTracking()
            .Where(u =>
                u.IsActive &&
                !db.Employees.Any(
                    e => e.UserId == u.UserId))
            .OrderBy(u => u.Username)
            .Select(u => new
            {
                u.UserId,
                u.Username
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    // ==========================================
    // Create employee
    // ==========================================

    [HttpPost]
    public async Task<IActionResult> Create(
        CreateEmployeeDto dto,
        CancellationToken ct)
    {
        if (!ValidType(dto.EmploymentType))
            return BadRequest(new
            {
                message = "Invalid employment type."
            });

        if (dto.UserId <= 0)
            return BadRequest(new
            {
                message = "A user account is required."
            });

        var employeeCode = dto.EmployeeCode.Trim();
        var workEmail = dto.WorkEmail.Trim();

        if (await db.Employees.AnyAsync(
            e => e.EmployeeCode == employeeCode
                 || e.WorkEmail == workEmail, ct))
            return Conflict(new
            {
                message = "Employee code or work email already used."
            });

        var userExists = await db.UserAccounts.AnyAsync(
            u => u.UserId == dto.UserId && u.IsActive,
            ct);

        if (!userExists)
            return BadRequest(new
            {
                message = "User account not found or inactive."
            });

        var alreadyAssigned = await db.Employees.AnyAsync(
            e => e.UserId == dto.UserId, ct);

        if (alreadyAssigned)
            return Conflict(new
            {
                message = "User account already assigned to an employee."
            });

        var employee = new Employee
        {
            UserId = dto.UserId,
            EmployeeCode = employeeCode,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            WorkEmail = workEmail,
            PersonalEmail = dto.PersonalEmail,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth?.Date,
            HireDate = dto.HireDate.Date,
            EmploymentType = dto.EmploymentType
        };

        db.Employees.Add(employee);

        await db.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(Get),
            new { id = employee.EmployeeId },
            Map(employee));
    }

    // ==========================================
    // Update employee
    // ==========================================

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        UpdateEmployeeDto dto,
        CancellationToken ct)
    {
        var employee = await db.Employees.FindAsync(
            new object[] { id }, ct);

        if (employee is null)
            return NotFound();

        if (!ValidType(dto.EmploymentType))
            return BadRequest(new
            {
                message = "Invalid employment type."
            });

        var workEmail = dto.WorkEmail.Trim();

        if (await db.Employees.AnyAsync(
            e => e.EmployeeId != id
                && e.WorkEmail == workEmail, ct))
            return Conflict(new
            {
                message = "Work email already used."
            });

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.WorkEmail = workEmail;
        employee.PersonalEmail = dto.PersonalEmail;
        employee.PhoneNumber = dto.PhoneNumber;
        employee.DateOfBirth = dto.DateOfBirth?.Date;
        employee.EmploymentType = dto.EmploymentType;
        employee.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return Ok(Map(employee));
    }

    // ==========================================
    // Employment status
    // ==========================================

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(
        int id,
        ChangeEmploymentStatusDto dto,
        CancellationToken ct)
    {
        if (dto.Status is not (
            "ACTIVE" or "ON_LEAVE" or "TERMINATED"))
            return BadRequest(new
            {
                message = "Invalid employment status."
            });

        var employee = await db.Employees.FindAsync(
            new object[] { id }, ct);

        if (employee is null)
            return NotFound();

        employee.EmploymentStatus = dto.Status;
        employee.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return NoContent();
    }

    // Keep your existing assignment and reportee
    // methods below this point.

    [HttpGet("{id:int}/assignments")]public async Task<IActionResult> Assignments(int id,CancellationToken ct){if(!await db.Employees.AnyAsync(x=>x.EmployeeId==id,ct))return NotFound();var rows=await db.EmployeeAssignments.AsNoTracking().Where(x=>x.EmployeeId==id).OrderByDescending(x=>x.EffectiveFrom).ToListAsync(ct);return Ok(rows.Select(MapAssignment));}
    private static AssignmentDto MapAssignment(EmployeeAssignment x)=>new(x.AssignmentId,x.EmployeeId,x.DepartmentId,x.DesignationId,x.ManagerId,x.EffectiveFrom,x.EffectiveTo);
    [HttpPost("{id:int}/assignments")]public async Task<IActionResult> CreateAssignment(int id,CreateAssignmentDto dto,CancellationToken ct){
        var day=dto.EffectiveFrom.Date;
        if(!await db.Employees.AnyAsync(x=>x.EmployeeId==id&&x.EmploymentStatus!="TERMINATED",ct))return BadRequest("Employee missing or terminated.");
        if(!await db.Departments.AnyAsync(x=>x.DepartmentId==dto.DepartmentId&&x.IsActive,ct)||!await db.Designations.AnyAsync(x=>x.DesignationId==dto.DesignationId&&x.IsActive,ct))return BadRequest("Invalid or inactive department/designation.");
        if(dto.ManagerId==id)return BadRequest("Employee cannot manage themselves.");
        if(dto.ManagerId!=null&&!await db.Employees.AnyAsync(x=>x.EmployeeId==dto.ManagerId&&x.EmploymentStatus=="ACTIVE",ct))return BadRequest("Invalid manager.");
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
        var current=await db.EmployeeAssignments.Where(x=>x.EmployeeId==id&&x.EffectiveTo==null).SingleOrDefaultAsync(ct);
        if(current!=null&&current.EffectiveFrom>=day)return Conflict("New assignment must start after current assignment.");
        if(await db.EmployeeAssignments.AnyAsync(x=>x.EmployeeId==id && (x.EffectiveFrom>=day || (x.EffectiveTo!=null && x.EffectiveTo>=day)),ct))return Conflict("Assignment date overlaps existing history.");
        if(current!=null)current.EffectiveTo=day.AddDays(-1);
        // Prevent indirect manager cycles using currently open organizational assignments.
        if(dto.ManagerId!=null){var seen=new HashSet<int>{id};int? manager=dto.ManagerId;
            while(manager!=null){if(!seen.Add(manager.Value))return Conflict("Reporting hierarchy cycle.");manager=await db.EmployeeAssignments.Where(x=>x.EmployeeId==manager&&x.EffectiveFrom<=day&&(x.EffectiveTo==null||x.EffectiveTo>=day)).Select(x=>x.ManagerId).FirstOrDefaultAsync(ct);}}
        var assignment=new EmployeeAssignment{EmployeeId=id,DepartmentId=dto.DepartmentId,DesignationId=dto.DesignationId,ManagerId=dto.ManagerId,EffectiveFrom=day};db.EmployeeAssignments.Add(assignment);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return Ok(MapAssignment(assignment));
    }
    [HttpGet("{id:int}/reportees")]public async Task<IActionResult> Reportees(int id,CancellationToken ct){if(!await db.Employees.AnyAsync(x=>x.EmployeeId==id,ct))return NotFound();var day=DateTime.UtcNow.Date;var ids=await db.EmployeeAssignments.Where(x=>x.ManagerId==id&&x.EffectiveFrom<=day&&(x.EffectiveTo==null||x.EffectiveTo>=day)).Select(x=>x.EmployeeId).ToListAsync(ct);return Ok(await db.Employees.AsNoTracking().Where(x=>ids.Contains(x.EmployeeId)).ToListAsync(ct));}


    [HttpPut("{id:int}/assignments/{assignmentId:int}")]
    public async Task<IActionResult> UpdateAssignment(
        int id,
        int assignmentId,
        UpdateAssignmentDto dto,
        CancellationToken ct)
    {
        var startDate = dto.EffectiveFrom.Date;

        // 1. Validate department and designation.
        var departmentExists = await db.Departments.AnyAsync(
            x => x.DepartmentId == dto.DepartmentId && x.IsActive,
            ct);

        var designationExists = await db.Designations.AnyAsync(
            x => x.DesignationId == dto.DesignationId && x.IsActive,
            ct);

        if (!departmentExists || !designationExists)
            return BadRequest("Invalid or inactive department/designation.");

        // 2. Validate manager.
        if (dto.ManagerId == id)
            return BadRequest("Employee cannot manage themselves.");

        if (dto.ManagerId.HasValue)
        {
            var managerExists = await db.Employees.AnyAsync(
                x => x.EmployeeId == dto.ManagerId.Value
                     && x.EmploymentStatus == "ACTIVE",
                ct);

            if (!managerExists)
                return BadRequest("Invalid or inactive manager.");
        }

        // 3. Use a transaction to protect assignment history.
        await using var transaction =
            await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

        // 4. Find the assignment belonging to this employee.
        var assignment = await db.EmployeeAssignments
            .FirstOrDefaultAsync(
                x => x.AssignmentId == assignmentId
                     && x.EmployeeId == id,
                ct);

        if (assignment == null)
            return NotFound("Employee assignment not found.");

        // 5. Ensure the corrected start date is valid.
        if (assignment.EffectiveTo.HasValue &&
            startDate > assignment.EffectiveTo.Value.Date)
        {
            return Conflict(
                "Start date cannot be after the assignment end date.");
        }

        // 6. Prevent overlapping assignment history.
        var endDate = assignment.EffectiveTo
            ?? DateTime.MaxValue.Date;

        var overlaps = await db.EmployeeAssignments.AnyAsync(
            x => x.EmployeeId == id
                 && x.AssignmentId != assignmentId
                 && x.EffectiveFrom <= endDate
                 && (x.EffectiveTo == null ||
                     x.EffectiveTo >= startDate),
            ct);

        if (overlaps)
            return Conflict(
                "Updated dates overlap another assignment.");

        // 7. Prevent circular reporting relationships.
        if (dto.ManagerId.HasValue)
        {
            var visited = new HashSet<int> { id };
            int? managerId = dto.ManagerId;

            while (managerId.HasValue)
            {
                if (!visited.Add(managerId.Value))
                    return Conflict("Reporting hierarchy cycle detected.");

                managerId = await db.EmployeeAssignments
                    .Where(x =>
                        x.EmployeeId == managerId.Value &&
                        x.EffectiveFrom <= startDate &&
                        (x.EffectiveTo == null ||
                         x.EffectiveTo >= startDate))
                    .Select(x => x.ManagerId)
                    .FirstOrDefaultAsync(ct);
            }
        }

        // 8. Update the existing assignment.
        assignment.DepartmentId = dto.DepartmentId;
        assignment.DesignationId = dto.DesignationId;
        assignment.ManagerId = dto.ManagerId;
        assignment.EffectiveFrom = startDate;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        // 9. Return updated assignment.
        return Ok(MapAssignment(assignment));
    }

}
