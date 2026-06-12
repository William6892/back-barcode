using BarcodeShippingSystem.DTOs;
using Microsoft.AspNetCore.Mvc;
using ShippingExitSystem.DTOs;
using ShippingExitSystem.Services;

namespace ShippingExitSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        try
        {
            var user = await _authService.RegisterAsync(dto);
            return Ok(new { id = user.Id, username = user.Username, email = user.Email });
        }
        catch (Exception ex)
        {
            var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "No inner exception";
            return BadRequest(new { error = ex.Message, innerError = innerMessage, stackTrace = ex.StackTrace });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        try
        {
            var token = await _authService.LoginAsync(dto);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }
}