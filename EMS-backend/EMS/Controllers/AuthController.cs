using EMS.DTOs.Auth;
using EMS.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace EMS.Controllers;
[ApiController, Route("api/auth")]
public class AuthController(IAuthService auth) : ControllerBase
{
    [AllowAnonymous, HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        var response = await auth.LoginAsync(dto, ct);
        return response is null ? Unauthorized(new { message = "Invalid credentials." }) : Ok(response);
    }
}
