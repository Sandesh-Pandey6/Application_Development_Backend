using Autopartspro.Application.DTOs.Auth;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Customer self-registration (2-step: personal + vehicle)</summary>
    [HttpPost("register/customer")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterDto dto)
    {
        var result = await _authService.RegisterCustomerAsync(dto);
        return Ok(result);
    }

    /// <summary>Staff self-registration (2-step: personal + employment)</summary>
    [HttpPost("register/staff")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterStaff([FromBody] StaffRegisterDto dto)
    {
        var result = await _authService.RegisterStaffAsync(dto);
        return Ok(result);
    }

    /// <summary>Login for Customer, Staff, Admin — returns JWT</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    /// <summary>Verify email with 6-digit OTP code</summary>
    [HttpPost("verify-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
    {
        var result = await _authService.VerifyOtpAsync(dto);
        return Ok(new { success = result, message = "Email verified successfully." });
    }

    /// <summary>Resend OTP code to email</summary>
    [HttpPost("resend-otp")]
    [AllowAnonymous]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
    {
        await _authService.ResendOtpAsync(dto);
        return Ok(new { message = "A new verification code has been sent to your email." });
    }

    /// <summary>Test protected route — Admin only</summary>
    [HttpGet("admin-only")]
    [Authorize(Roles = "Admin")]
    public IActionResult AdminOnly() =>
        Ok(new { message = "Welcome, Admin!" });

    /// <summary>Test protected route — Staff and Admin</summary>
    [HttpGet("staff-only")]
    [Authorize(Roles = "Staff,Admin")]
    public IActionResult StaffOnly() =>
        Ok(new { message = "Welcome, Staff/Admin!" });

    /// <summary>Test protected route — any authenticated user</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var name = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        return Ok(new { userId, role, name });
    }
}