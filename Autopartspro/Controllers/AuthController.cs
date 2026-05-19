using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
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
            if (string.IsNullOrWhiteSpace(dto.FullName) ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Full name, email, and password are required." });
            }

            try
            {
                var message = await _authService.RegisterAsync(dto);
                return Ok(new { message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Email and password are required." });

            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
                ?? User.FindFirstValue(JwtRegisteredClaimNames.Email)
                ?? User.FindFirstValue("email");
            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized(new { message = "Invalid or expired session. Please sign in again." });
            var result = await _authService.GetCurrentUserAsync(email);
            return Ok(result);
        }
    }
}
