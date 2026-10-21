using EMS.DB;
using EMS.DTOs.Organization;
using EMS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace EMS.Controllers;
[ApiController,Route("api/designations"),Authorize(Roles="SYSTEM_ADMIN,HR_MANAGER")]
public class DesignationsController(AppDbContext db):ControllerBase {
    private static DesignationDto Map(Designation x)=>new(x.DesignationId,x.Title,x.Code,x.Description,x.IsActive);
    [HttpGet,Authorize] public async Task<IActionResult> List(CancellationToken ct)=>Ok((await db.Designations.AsNoTracking().OrderBy(x=>x.Title).Take(200).ToListAsync(ct)).Select(Map));
    [HttpGet("{id:int}"),Authorize] public async Task<IActionResult> Get(int id,CancellationToken ct){var x=await db.Designations.FindAsync(new object[]{id},ct);return x==null?NotFound():Ok(Map(x));}
    [HttpPost] public async Task<IActionResult> Create(CreateDesignationDto dto,CancellationToken ct){
        if(string.IsNullOrWhiteSpace(dto.Title)||string.IsNullOrWhiteSpace(dto.Code))return BadRequest("Title and code required.");
        if(await db.Designations.AnyAsync(x=>x.Code==dto.Code.Trim(),ct))return Conflict("Designation code exists.");
        var x=new Designation{Title=dto.Title.Trim(),Code=dto.Code.Trim(),Description=dto.Description};db.Designations.Add(x);await db.SaveChangesAsync(ct);return CreatedAtAction(nameof(Get),new{id=x.DesignationId},Map(x));
    }
    [HttpPut("{id:int}")]public async Task<IActionResult> Update(int id,UpdateDesignationDto dto,CancellationToken ct){var x=await db.Designations.FindAsync(new object[]{id},ct);if(x==null)return NotFound();
        if(string.IsNullOrWhiteSpace(dto.Title)||string.IsNullOrWhiteSpace(dto.Code))return BadRequest("Title and code required.");
        if(await db.Designations.AnyAsync(d=>d.DesignationId!=id&&d.Code==dto.Code.Trim(),ct))return Conflict("Designation code exists.");
        x.Title=dto.Title.Trim();x.Code=dto.Code.Trim();x.Description=dto.Description;await db.SaveChangesAsync(ct);return Ok(Map(x));}
    [HttpPatch("{id:int}/status")]public async Task<IActionResult> Status(int id,SetStatusDto dto,CancellationToken ct){var x=await db.Designations.FindAsync(new object[]{id},ct);if(x==null)return NotFound();x.IsActive=dto.IsActive;await db.SaveChangesAsync(ct);return NoContent();}
}
