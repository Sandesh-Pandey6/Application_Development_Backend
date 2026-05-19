using System.Security.Claims;
using Autopartspro.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize(Roles = "Admin,Staff")]
public class NotificationsController : ControllerBase
{
    private readonly IUserNotificationService _notifications;

    public NotificationsController(IUserNotificationService notifications)
    {
        _notifications = notifications;
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(id, out var userId) ? userId : null;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? type)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _notifications.GetForUserAsync(userId.Value, type);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await _notifications.GetForUserAsync(userId.Value, "All");
        return Ok(new { totalUnread = result.TotalUnread });
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _notifications.MarkReadAsync(id, userId.Value);
            return Ok(new { message = "Notification marked as read." });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Notification not found." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _notifications.MarkAllReadAsync(userId.Value);
        return Ok(new { message = "All notifications marked as read." });
    }
}
