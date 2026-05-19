using System.Security.Claims;
using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Dtos.Staff;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "Staff,Admin")]
public class StaffPortalController : ControllerBase
{
    private readonly IUserPasswordService _passwords;
    private readonly IStaffProfileService _staffProfile;
    private readonly IUserProfileImageService _profileImages;

    public StaffPortalController(
        IUserPasswordService passwords,
        IStaffProfileService staffProfile,
        IUserProfileImageService profileImages)
    {
        _passwords = passwords;
        _staffProfile = staffProfile;
        _profileImages = profileImages;
    }

    [HttpGet("profile")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        try
        {
            return Ok(await _staffProfile.GetProfileAsync(userId.Value));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("profile")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateStaffProfileDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        try
        {
            return Ok(await _staffProfile.UpdateProfileAsync(userId.Value, dto));
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

    [HttpPost("profile/photo")]
    [Authorize(Roles = "Staff")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes)]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        try
        {
            ImageUploadRules.Validate(file);
            await using var stream = file!.OpenReadStream();
            await _profileImages.UploadAsync(userId.Value, stream, file.FileName, file.ContentType);
            return Ok(await _staffProfile.GetProfileAsync(userId.Value));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("profile/photo")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid session. Please sign in again." });

        try
        {
            await _profileImages.RemoveAsync(userId.Value);
            return Ok(await _staffProfile.GetProfileAsync(userId.Value));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
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
