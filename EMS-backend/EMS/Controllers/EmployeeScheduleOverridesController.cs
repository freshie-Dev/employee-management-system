using System.Security.Claims;
using EMS.DTOs.ScheduleOverrides;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace EMS.Controllers;
[ApiController]
[Route("api/employees/{employeeId:int}")]
[Authorize(Roles="SYSTEM_ADMIN,HR_MANAGER")]
public sealed class EmployeeScheduleOverridesController(IScheduleOverrideService service):ControllerBase
{
 // Reads restricted to HR/Admin until employee/manager scope authorization is implemented.
 [HttpGet("schedule-overrides")]
 public async Task<IActionResult> List(int employeeId,[FromQuery] DateOnly? fromDate,[FromQuery] DateOnly? toDate,[FromQuery] bool includeCancelled,CancellationToken ct)
  =>Ok(await service.ListAsync(employeeId,fromDate,toDate,includeCancelled,ct));
 [HttpGet("schedule-overrides/{overrideId:long}")]
 public async Task<IActionResult> Get(int employeeId,long overrideId,CancellationToken ct)
  =>(await service.GetAsync(employeeId,overrideId,ct)) is { } r ? Ok(r):NotFound();
 [HttpGet("schedule")]
 public async Task<IActionResult> Resolve(int employeeId,[FromQuery] DateOnly workDate,CancellationToken ct)
  =>workDate==default?BadRequest(new{message="workDate is required."}):Ok(await service.ResolveAsync(employeeId,workDate,ct));
 [HttpPost("schedule-overrides")]
 public async Task<IActionResult> Create(int employeeId,CreateScheduleOverrideDto dto,CancellationToken ct)
 {
  if(dto.WorkDate==default)return BadRequest(new{message="workDate is required."});
  var raw=User.FindFirstValue(ClaimTypes.NameIdentifier);
  if(!int.TryParse(raw,out var actorId))return Unauthorized(new{message="JWT must contain numeric NameIdentifier (UserId)."});
  try {var r=await service.CreateAsync(employeeId,actorId,dto,ct);
    return CreatedAtAction(nameof(Get),new{employeeId,overrideId=r.OverrideId},r);}
  catch(SqlException ex) when(ex.Number is >=51301 and <=51307){return SqlError(ex);}
 }
 [HttpPut("schedule-overrides/{overrideId:long}")]
 public async Task<IActionResult> Update(int employeeId,long overrideId,UpdateScheduleOverrideDto dto,CancellationToken ct)
 {try{return Ok(await service.UpdateAsync(employeeId,overrideId,dto,ct));}
  catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
  catch(SqlException ex) when(ex.Number is >=51301 and <=51307){return SqlError(ex);}}
 [HttpPatch("schedule-overrides/{overrideId:long}/cancel")]
 public async Task<IActionResult> Cancel(int employeeId,long overrideId,CancelScheduleOverrideDto dto,CancellationToken ct)
 {try{return Ok(await service.CancelAsync(employeeId,overrideId,dto,ct));}
  catch(ArgumentException ex){return BadRequest(new{message=ex.Message});}
  catch(SqlException ex) when(ex.Number is >=51301 and <=51307){return SqlError(ex);}}
 private IActionResult SqlError(SqlException ex)=>ex.Number switch{
  51301 or 51302=>NotFound(new{message=ex.Message}),
  51304 or 51305 or 51306=>Conflict(new{message=ex.Message}),
  51307=>Forbid(),
  _=>BadRequest(new{message=ex.Message})};
}
