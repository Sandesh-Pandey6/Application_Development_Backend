using System.Security.Claims;
using Autopartspro.Application.DOTs.customer;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/customer")]
[Authorize(Roles = "Customer")]
public class CustomerPortalController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ISalesInvoiceService _salesInvoiceService;
    private readonly AppDbContext _db;

    public CustomerPortalController(
        ICustomerService customerService,
        ISalesInvoiceService salesInvoiceService,
        AppDbContext db)
    {
        _customerService = customerService;
        _salesInvoiceService = salesInvoiceService;
        _db = db;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        return Ok(await _customerService.GetProfileAsync(userId.Value));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            var profile = await _customerService.UpdateProfileAsync(userId.Value, dto);
            return Ok(profile);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("vehicles")]
    public async Task<IActionResult> AddVehicle([FromBody] CreateVehicleDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            var vehicle = await _customerService.AddVehicleAsync(userId.Value, dto);
            return Ok(vehicle);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid vehicleId, [FromBody] UpdateVehicleDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            var vehicle = await _customerService.UpdateVehicleAsync(userId.Value, vehicleId, dto);
            return Ok(vehicle);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpDelete("vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> DeleteVehicle(Guid vehicleId)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            await _customerService.DeleteVehicleAsync(userId.Value, vehicleId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("appointments")]
    public async Task<IActionResult> GetAppointments()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var list = await _db.Appointments
            .Where(a => a.CustomerId == userId.Value)
            .OrderByDescending(a => a.PreferredDate)
            .ThenByDescending(a => a.PreferredTime)
            .ToListAsync();

        return Ok(list.Select(MapAppointment));
    }

    [HttpPost("appointments")]
    public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        if (string.IsNullOrWhiteSpace(dto.ServiceType))
            return BadRequest(new { message = "Service type is required." });
        if (!DateOnly.TryParse(dto.Date, out var preferredDate))
            return BadRequest(new { message = "Invalid appointment date." });

        var notes = BuildNotesWithVehicle(dto.Vehicle, dto.Notes);
        var appointment = new Appointment
        {
            CustomerId = userId.Value,
            ServiceType = dto.ServiceType.Trim(),
            PreferredDate = preferredDate,
            PreferredTime = ParseAppointmentTime(dto.Time),
            Notes = notes,
            Status = AppointmentStatus.Pending
        };

        _db.Appointments.Add(appointment);
        _db.Notifications.Add(new Notification
        {
            UserId = userId.Value,
            Message = $"Your {dto.ServiceType} appointment on {preferredDate:yyyy-MM-dd} is pending confirmation.",
            Type = NotificationType.AppointmentConfirmation
        });
        await _db.SaveChangesAsync();

        return Ok(MapAppointment(appointment));
    }

    [HttpDelete("appointments/{id:guid}")]
    public async Task<IActionResult> CancelAppointment(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == userId.Value);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return BadRequest(new { message = "This appointment cannot be cancelled." });

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapAppointment(appointment));
    }

    [HttpGet("part-requests")]
    public async Task<IActionResult> GetPartRequests()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var list = await _db.PartRequests
            .Where(p => p.CustomerId == userId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(MapPartRequest));
    }

    [HttpPost("part-requests")]
    public async Task<IActionResult> CreatePartRequest([FromBody] CreatePartRequestDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        if (string.IsNullOrWhiteSpace(dto.PartName))
            return BadRequest(new { message = "Part name is required." });

        var request = new PartRequest
        {
            CustomerId = userId.Value,
            PartName = dto.PartName.Trim(),
            PartDescription = string.IsNullOrWhiteSpace(dto.PartDescription) ? dto.PartName.Trim() : dto.PartDescription.Trim(),
            VehicleModel = dto.VehicleModel.Trim(),
            UrgencyLevel = ParseUrgency(dto.UrgencyLevel),
            Status = PartRequestStatus.Pending
        };

        _db.PartRequests.Add(request);
        _db.Notifications.Add(new Notification
        {
            UserId = userId.Value,
            Message = $"Part request for \"{request.PartName}\" has been submitted.",
            Type = NotificationType.General
        });
        await _db.SaveChangesAsync();

        return Ok(MapPartRequest(request));
    }

    [HttpDelete("part-requests/{id:guid}")]
    public async Task<IActionResult> DeletePartRequest(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var request = await _db.PartRequests
            .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == userId.Value);
        if (request == null)
            return NotFound(new { message = "Part request not found." });

        if (request.Status != PartRequestStatus.Pending)
            return BadRequest(new { message = "Only pending requests can be deleted." });

        _db.PartRequests.Remove(request);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> GetReviews()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var list = await _db.Reviews
            .Where(r => r.CustomerId == userId.Value)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(MapReview));
    }

    [HttpPost("reviews")]
    public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        if (dto.Rating is < 1 or > 5)
            return BadRequest(new { message = "Rating must be between 1 and 5." });
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Review title is required." });

        var review = new Review
        {
            CustomerId = userId.Value,
            Rating = dto.Rating,
            Title = dto.Title.Trim(),
            Description = dto.Content.Trim(),
            ReviewCategory = ParseReviewCategory(dto.Service)
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();
        return Ok(MapReview(review));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var list = await _db.Notifications
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return Ok(list.Select(MapNotification));
    }

    [HttpPatch("notifications/{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);
        if (notification == null)
            return NotFound(new { message = "Notification not found." });

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapNotification(notification));
    }

    [HttpPost("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var unread = await _db.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(new { updated = unread.Count });
    }

    [HttpDelete("notifications/{id:guid}")]
    public async Task<IActionResult> DeleteNotification(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value);
        if (notification == null)
            return NotFound(new { message = "Notification not found." });

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var invoices = await _db.SalesInvoices
            .Include(s => s.Items)
            .ThenInclude(i => i.Part)
            .Where(s => s.CustomerId == userId.Value)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        return Ok(invoices.Select(MapPurchase));
    }

    [HttpGet("purchases/{id:guid}/download")]
    public async Task<IActionResult> DownloadPurchase(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var ownsInvoice = await _db.SalesInvoices.AsNoTracking()
            .AnyAsync(s => s.Id == id && s.CustomerId == userId.Value);
        if (!ownsInvoice)
            return NotFound(new { message = "Invoice not found." });

        try
        {
            var pdf = await _salesInvoiceService.GetInvoicePdfBytesAsync(id);
            var number = await _db.SalesInvoices.AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => s.InvoiceNumber)
                .FirstAsync();
            return File(pdf, "application/pdf", $"{number}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Invoice not found." });
        }
    }

    [HttpGet("loyalty")]
    public async Task<IActionResult> GetLoyalty()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var invoices = await _db.SalesInvoices
            .Where(s => s.CustomerId == userId.Value)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var totalPoints = invoices.Sum(i => (int)Math.Floor(i.TotalAmount / 100m));
        var discountPercentage = invoices.Any(i => i.TotalAmount > 5000) ? 10 : 0;

        var transactions = invoices.Select(i => new
        {
            date = i.SaleDate,
            description = $"Purchase {i.InvoiceNumber}",
            points = (int)Math.Floor(i.TotalAmount / 100m),
            type = "earned"
        }).ToList();

        return Ok(new
        {
            totalPoints,
            discountPercentage,
            totalPurchases = invoices.Count,
            transactions
        });
    }

    [HttpPost("loyalty/redeem")]
    public IActionResult RedeemPoints([FromBody] RedeemPointsDto dto)
    {
        return BadRequest(new { message = "Point redemption is applied automatically at checkout. Contact support for assistance." });
    }

    private Guid? GetUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    private static IActionResult UnauthorizedSession() =>
        new UnauthorizedObjectResult(new { message = "Invalid session. Please sign in again." });

    private static object MapAppointment(Appointment a) => new
    {
        id = a.Id.ToString(),
        service = a.ServiceType,
        date = a.PreferredDate.ToString("yyyy-MM-dd"),
        time = FormatTime(a.PreferredTime),
        status = MapAppointmentStatus(a.Status),
        vehicle = ExtractVehicle(a.Notes) ?? "—",
        amount = "—"
    };

    private static object MapPartRequest(PartRequest p) => new
    {
        id = p.Id.ToString(),
        partName = p.PartName,
        vehicle = p.VehicleModel,
        urgency = p.UrgencyLevel.ToString(),
        status = MapPartRequestStatus(p.Status),
        date = p.CreatedAt,
        estimatedPrice = (string?)null
    };

    private static object MapReview(Review r) => new
    {
        id = r.Id.ToString(),
        rating = r.Rating,
        title = r.Title,
        content = r.Description,
        service = MapReviewServiceLabel(r.ReviewCategory),
        date = r.CreatedAt,
        helpful = 0
    };

    private static object MapNotification(Notification n) => new
    {
        id = n.Id.ToString(),
        title = MapNotificationTitle(n.Type),
        message = n.Message,
        type = MapNotificationTypeKey(n.Type),
        isRead = n.IsRead,
        createdAt = n.CreatedAt
    };

    private static object MapPurchase(SalesInvoice s)
    {
        var itemNames = s.Items.Select(i => i.Part?.PartName ?? "Part").ToList();
        var itemsLabel = itemNames.Count == 0
            ? "—"
            : string.Join(", ", itemNames.Take(3)) + (itemNames.Count > 3 ? $" +{itemNames.Count - 3} more" : "");

        var original = s.SubTotal + s.DiscountAmount;
        var discountPct = original > 0 && s.DiscountAmount > 0
            ? Math.Round(s.DiscountAmount / original * 100, 0)
            : (s.TotalAmount > 5000 ? 10m : 0m);

        return new
        {
            id = s.InvoiceNumber,
            invoiceId = s.Id.ToString(),
            date = s.SaleDate,
            items = itemsLabel,
            originalAmount = original,
            discountAmount = s.DiscountAmount,
            discountPercentage = discountPct,
            amount = s.TotalAmount,
            status = s.PaymentStatus.ToString()
        };
    }

    private static string MapAppointmentStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Completed => "Completed",
        AppointmentStatus.Cancelled => "Cancelled",
        _ => "Upcoming"
    };

    private static string MapPartRequestStatus(PartRequestStatus status) => status switch
    {
        PartRequestStatus.Approved => "Available",
        PartRequestStatus.Rejected => "Rejected",
        _ => "Searching"
    };

    private static string MapReviewServiceLabel(ReviewCategory category) => category switch
    {
        ReviewCategory.PartsPurchased => "Parts Purchased",
        ReviewCategory.OverallExperience => "Overall Experience",
        _ => "Service Received"
    };

    private static ReviewCategory ParseReviewCategory(string service)
    {
        if (service.Contains("Part", StringComparison.OrdinalIgnoreCase))
            return ReviewCategory.PartsPurchased;
        if (service.Contains("Overall", StringComparison.OrdinalIgnoreCase))
            return ReviewCategory.OverallExperience;
        return ReviewCategory.ServiceReceived;
    }

    private static UrgencyLevel ParseUrgency(string urgency)
    {
        if (urgency.Contains("High", StringComparison.OrdinalIgnoreCase))
            return UrgencyLevel.High;
        if (urgency.Contains("Low", StringComparison.OrdinalIgnoreCase))
            return UrgencyLevel.Low;
        return UrgencyLevel.Medium;
    }

    private static TimeOnly ParseAppointmentTime(string time)
    {
        if (string.IsNullOrWhiteSpace(time))
            return new TimeOnly(10, 0);
        if (time.Contains("Evening", StringComparison.OrdinalIgnoreCase) || time.Contains("4 PM", StringComparison.OrdinalIgnoreCase))
            return new TimeOnly(16, 0);
        if (time.Contains("Afternoon", StringComparison.OrdinalIgnoreCase) || time.Contains("1 PM", StringComparison.OrdinalIgnoreCase))
            return new TimeOnly(13, 0);
        return new TimeOnly(10, 0);
    }

    private static string FormatTime(TimeOnly time) =>
        time.ToString("h:mm tt", System.Globalization.CultureInfo.InvariantCulture);

    private static string? ExtractVehicle(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith("Vehicle:", StringComparison.OrdinalIgnoreCase))
            return null;
        var firstLine = notes.Split('\n')[0];
        return firstLine["Vehicle:".Length..].Trim();
    }

    private static string? BuildNotesWithVehicle(string? vehicle, string? notes)
    {
        if (string.IsNullOrWhiteSpace(vehicle))
            return string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        var combined = $"Vehicle: {vehicle.Trim()}";
        if (!string.IsNullOrWhiteSpace(notes))
            combined += $"\n{notes.Trim()}";
        return combined;
    }

    private static string MapNotificationTitle(NotificationType type) => type switch
    {
        NotificationType.AppointmentConfirmation => "Appointment",
        NotificationType.LowStock => "Stock Alert",
        NotificationType.CreditReminder => "Payment Reminder",
        _ => "Notification"
    };

    private static string MapNotificationTypeKey(NotificationType type) => type switch
    {
        NotificationType.AppointmentConfirmation => "appointment",
        NotificationType.LowStock => "stock",
        NotificationType.CreditReminder => "service",
        _ => "general"
    };

}
