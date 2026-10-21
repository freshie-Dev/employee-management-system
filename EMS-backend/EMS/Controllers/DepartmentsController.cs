using EMS.DB;
using EMS.DTOs.Organization;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EMS.Controllers;
[ApiController, Route("api/departments"), Authorize(Roles="SYSTEM_ADMIN,HR_MANAGER")]
public class DepartmentsController(AppDbContext db) : ControllerBase {
    private static DepartmentDto Map(Department d) => new(d.DepartmentId,d.Name,d.Code,d.ParentDepartmentId,d.IsActive);
    [HttpGet, Authorize] public async Task<IActionResult> List([FromQuery] string? search, CancellationToken ct) {
        var q=db.Departments.AsNoTracking().AsQueryable();
        if(!string.IsNullOrWhiteSpace(search)) q=q.Where(x=>x.Name.Contains(search)||x.Code.Contains(search));
        return Ok((await q.OrderBy(x=>x.Name).Take(200).ToListAsync(ct)).Select(Map));
    }
    [HttpGet("{id:int}"), Authorize] public async Task<IActionResult> Get(int id,CancellationToken ct) { var x=await db.Departments.FindAsync(new object[]{id},ct);return x==null?NotFound():Ok(Map(x)); }
    [HttpPost] public async Task<IActionResult> Create(CreateDepartmentDto dto,CancellationToken ct) {
        if(string.IsNullOrWhiteSpace(dto.Name)||string.IsNullOrWhiteSpace(dto.Code))return BadRequest("Name and code required.");
        if(await db.Departments.AnyAsync(x=>x.Code==dto.Code.Trim(),ct))return Conflict("Department code exists.");
        if(dto.ParentDepartmentId!=null && !await db.Departments.AnyAsync(x=>x.DepartmentId==dto.ParentDepartmentId && x.IsActive,ct))return BadRequest("Invalid parent department.");
        var x=new Department {Name=dto.Name.Trim(),Code=dto.Code.Trim(),ParentDepartmentId=dto.ParentDepartmentId};db.Departments.Add(x);await db.SaveChangesAsync(ct);return CreatedAtAction(nameof(Get),new{id=x.DepartmentId},Map(x));
    }
    [HttpPut("{id:int}")] public async Task<IActionResult> Update(int id,UpdateDepartmentDto dto,CancellationToken ct) {
        var x=await db.Departments.FindAsync(new object[]{id},ct);if(x==null)return NotFound();
        if(string.IsNullOrWhiteSpace(dto.Name)||string.IsNullOrWhiteSpace(dto.Code))return BadRequest("Name and code required.");
        if(await db.Departments.AnyAsync(d=>d.Code==dto.Code.Trim()&&d.DepartmentId!=id,ct))return Conflict("Department code exists.");
        if(dto.ParentDepartmentId==id)return BadRequest("A department cannot parent itself.");
        if(dto.ParentDepartmentId!=null) {
            var seen=new HashSet<int>{id};int? parent=dto.ParentDepartmentId;
            while(parent!=null) {if(!seen.Add(parent.Value))return BadRequest("Circular department hierarchy."); var current=await db.Departments.AsNoTracking().FirstOrDefaultAsync(d=>d.DepartmentId==parent,ct);if(current==null||!current.IsActive)return BadRequest("Invalid parent department.");parent=current.ParentDepartmentId;}
        }
        x.Name=dto.Name.Trim();x.Code=dto.Code.Trim();x.ParentDepartmentId=dto.ParentDepartmentId;await db.SaveChangesAsync(ct);return Ok(Map(x));
    }
    [HttpPatch("{id:int}/status")] public async Task<IActionResult> Status(int id,SetStatusDto dto,CancellationToken ct) {
        var x=await db.Departments.FindAsync(new object[]{id},ct);if(x==null)return NotFound();
        if(!dto.IsActive && await db.Departments.AnyAsync(d=>d.ParentDepartmentId==id && d.IsActive,ct))return Conflict("Deactivate child departments first.");
        x.IsActive=dto.IsActive;await db.SaveChangesAsync(ct);return NoContent();
    }
}
