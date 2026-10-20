using System.Security.Claims;
using EMS.DTOs.Users;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EMS.Controllers;
[ApiController, Route("api/users"), Authorize(Roles = "SYSTEM_ADMIN")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await users.GetAllAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var result = await users.GetByIdAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto, CancellationToken ct)
    {
        try
        {
            var result = await users.CreateAsync(dto, ct);
            return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
        }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }

    [HttpPatch("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto, CancellationToken ct)
    {
        try
        {
            var result = await users.UpdateAsync(id, dto, ct);
            return result is null ? NotFound() : Ok(result);
        }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }

    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> Status(int id, UpdateUserStatusDto dto, CancellationToken ct)
    {
        if (!dto.IsActive && IsSelf(id)) return BadRequest(new { message = "Cannot deactivate your own account." });
        try { return await users.UpdateStatusAsync(id, dto.IsActive, ct) ? NoContent() : NotFound(); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }

    [HttpPut("{id:int}/roles")]
    public async Task<IActionResult> Roles(int id, UpdateUserRolesDto dto, CancellationToken ct)
    {
        if (IsSelf(id)) return BadRequest(new { message = "Cannot modify your own roles." });
        try { return await users.UpdateRolesAsync(id, dto, ct) ? NoContent() : NotFound(); }
        catch (ArgumentException e) { return BadRequest(new { message = e.Message }); }
        catch (InvalidOperationException e) { return Conflict(new { message = e.Message }); }
    }

    private bool IsSelf(int id) => User.FindFirstValue(ClaimTypes.NameIdentifier) == id.ToString();
}
