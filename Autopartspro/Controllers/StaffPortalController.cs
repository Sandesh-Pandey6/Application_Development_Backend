using System.Security.Claims;
using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Staff,Admin")]
public class StaffPortalController : ControllerBase
{
    private readonly IUserPasswordService _passwords;

    public StaffPortalController(IUserPasswordService passwords)
    {
        _passwords = passwords;
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        try
        {
            var profile = await _passwords.ChangePasswordAsync(
                userId.Value,
                dto,
                RoleType.Staff,
                RoleType.Admin);

            return Ok(new
            {
                message = "Password updated successfully.",
                mustChangePassword = profile.MustChangePassword,
            });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claim, out var id) ? id : null;
    }
}
