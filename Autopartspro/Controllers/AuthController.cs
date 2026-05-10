using Autopartspro.Application.DOTs.auth;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Autopartspro.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var message = await _authService.RegisterAsync(dto);
            return Ok(new { message });
        }

        // POST api/auth/verify-email
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyEmailOtpAsync(dto);
            return Ok(result);
        }

        // POST api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var message = await _authService.LoginAsync(dto);
            return Ok(new { message });
        }

        // POST api/auth/verify-login
        [HttpPost("verify-login")]
        public async Task<IActionResult> VerifyLogin([FromBody] VerifyOtpDto dto)
        {
            var result = await _authService.VerifyLoginOtpAsync(dto);
            return Ok(result);
        }

        // POST api/auth/resend-otp
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            var message = await _authService.ResendOtpAsync(dto);
            return Ok(new { message });
        }

        // POST api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var message = await _authService.ForgotPasswordAsync(dto);
            return Ok(new { message });
        }

        // POST api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var message = await _authService.ResetPasswordAsync(dto);
            return Ok(new { message });
        }

        // GET api/auth/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                ?? throw new Exception("Unauthorized.");
            var result = await _authService.GetCurrentUserAsync(email);
            return Ok(result);
        }
    }
}