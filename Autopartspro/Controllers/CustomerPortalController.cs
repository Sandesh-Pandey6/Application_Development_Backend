using System.Security.Claims;
using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Dtos.Customer;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure;
using Autopartspro.Infrastructure.Data;
using Autopartspro.Infrastructure.Services;
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
    private readonly IUserNotificationService _notifications;
    private readonly IAppointmentSchedulingService _scheduling;
    private readonly IUserPasswordService _passwords;
    private readonly IUserProfileImageService _profileImages;
    private readonly AppDbContext _db;

    public CustomerPortalController(
        ICustomerService customerService,
        ISalesInvoiceService salesInvoiceService,
        IUserNotificationService notifications,
        IAppointmentSchedulingService scheduling,
        IUserPasswordService passwords,
        IUserProfileImageService profileImages,
        AppDbContext db)
    {
        _customerService = customerService;
        _salesInvoiceService = salesInvoiceService;
        _notifications = notifications;
        _scheduling = scheduling;
        _passwords = passwords;
        _profileImages = profileImages;
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

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            var profile = await _passwords.ChangePasswordAsync(userId.Value, dto, RoleType.Customer);
            return Ok(new
            {
                message = "Password updated successfully.",
                mustChangePassword = profile.MustChangePassword,
                profile,
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

    [HttpPost("me/photo")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes)]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? file)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            ImageUploadRules.Validate(file);
            await using var stream = file!.OpenReadStream();
            await _profileImages.UploadAsync(userId.Value, stream, file.FileName, file.ContentType);
            return Ok(await _customerService.GetProfileAsync(userId.Value));
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

    [HttpDelete("me/photo")]
    public async Task<IActionResult> DeleteProfilePhoto()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();
        try
        {
            await _profileImages.RemoveAsync(userId.Value);
            return Ok(await _customerService.GetProfileAsync(userId.Value));
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        var preferredTime = _scheduling.ParseTimeSlot(dto.Time);
        var appointment = new Appointment
        {
            CustomerId = userId.Value,
            ServiceType = dto.ServiceType.Trim(),
            PreferredDate = preferredDate,
            PreferredTime = preferredTime,
            Notes = notes,
            Status = AppointmentStatus.Pending
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            userId.Value,
            $"Your {dto.ServiceType} appointment on {preferredDate:yyyy-MM-dd} is pending confirmation.",
            NotificationType.AppointmentConfirmation);

        await _notifications.NotifyAdminsAndStaffAsync(
            $"New appointment request: {dto.ServiceType} on {preferredDate:yyyy-MM-dd}.",
            NotificationType.General);

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
        appointment.ProposedDate = null;
        appointment.ProposedTime = null;
        appointment.StaffNotes = null;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(MapAppointment(appointment));
    }

    [HttpPost("appointments/{id:guid}/accept-reschedule")]
    public async Task<IActionResult> AcceptReschedule(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == userId.Value);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });
        if (appointment.Status != AppointmentStatus.RescheduleProposed)
            return BadRequest(new { message = "No reschedule is waiting for your response." });
        if (!appointment.ProposedDate.HasValue || !appointment.ProposedTime.HasValue)
            return BadRequest(new { message = "Invalid reschedule proposal." });

        if (await _scheduling.IsProposedSlotFullAsync(
                appointment.ProposedDate.Value,
                appointment.ProposedTime.Value,
                appointment.Id))
            return BadRequest(new { message = "The proposed time is no longer available. Contact the service centre." });

        appointment.PreferredDate = appointment.ProposedDate.Value;
        appointment.PreferredTime = appointment.ProposedTime.Value;
        appointment.ProposedDate = null;
        appointment.ProposedTime = null;
        appointment.StaffNotes = null;
        appointment.Status = AppointmentStatus.Confirmed;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var when = $"{appointment.PreferredDate:yyyy-MM-dd} at {_scheduling.FormatTimeSlot(appointment.PreferredTime)}";
        await _notifications.NotifyUserAsync(
            userId.Value,
            $"You accepted the new appointment time: {when}.",
            NotificationType.AppointmentConfirmation);
        await _notifications.NotifyAdminsAndStaffAsync(
            $"Customer accepted reschedule for {appointment.ServiceType} on {when}.",
            NotificationType.General);

        return Ok(MapAppointment(appointment));
    }

    [HttpPost("appointments/{id:guid}/decline-reschedule")]
    public async Task<IActionResult> DeclineReschedule(Guid id)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var appointment = await _db.Appointments
            .FirstOrDefaultAsync(a => a.Id == id && a.CustomerId == userId.Value);
        if (appointment == null)
            return NotFound(new { message = "Appointment not found." });
        if (appointment.Status != AppointmentStatus.RescheduleProposed)
            return BadRequest(new { message = "No reschedule is waiting for your response." });

        appointment.Status = AppointmentStatus.Cancelled;
        appointment.ProposedDate = null;
        appointment.ProposedTime = null;
        appointment.StaffNotes = null;
        appointment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.NotifyAdminsAndStaffAsync(
            $"Customer declined the proposed reschedule for {appointment.ServiceType} ({appointment.PreferredDate:yyyy-MM-dd}).",
            NotificationType.General);

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
        await _db.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            userId.Value,
            $"Part request for \"{request.PartName}\" has been submitted. We will notify you when it is available.",
            NotificationType.General);

        await _notifications.NotifyAdminsAndStaffAsync(
            $"Customer requested part: \"{request.PartName}\" ({request.VehicleModel}, urgency: {request.UrgencyLevel}).",
            NotificationType.General);

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
            .Include(r => r.RelatedInvoice)
            .ThenInclude(i => i!.Items)
            .ThenInclude(li => li.Part)
            .Where(r =>
                r.CustomerId == userId.Value &&
                r.RelatedInvoiceId != null &&
                r.ReviewCategory == ReviewCategory.PartsPurchased)
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
        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest(new { message = "Review details are required." });
        if (dto.InvoiceId == Guid.Empty)
            return BadRequest(new { message = "Select a purchase to review." });

        var invoice = await _db.SalesInvoices
            .Include(s => s.Items)
            .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(s => s.Id == dto.InvoiceId && s.CustomerId == userId.Value);
        if (invoice == null)
            return BadRequest(new { message = "You can only review parts from your own purchases." });
        if (!invoice.Items.Any(i => i.PartId != Guid.Empty))
            return BadRequest(new { message = "This purchase has no parts to review." });

        var alreadyReviewed = await _db.Reviews.AnyAsync(r =>
            r.CustomerId == userId.Value && r.RelatedInvoiceId == invoice.Id);
        if (alreadyReviewed)
            return BadRequest(new { message = "You have already reviewed this purchase." });

        var review = new Review
        {
            CustomerId = userId.Value,
            Rating = dto.Rating,
            Title = dto.Title.Trim(),
            Description = dto.Content.Trim(),
            ReviewCategory = ReviewCategory.PartsPurchased,
            RelatedInvoiceId = invoice.Id
        };

        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        var saved = await _db.Reviews
            .Include(r => r.RelatedInvoice)
            .ThenInclude(i => i!.Items)
            .ThenInclude(li => li.Part)
            .FirstAsync(r => r.Id == review.Id);

        return Ok(MapReview(saved));
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

    [HttpGet("parts")]
    public async Task<IActionResult> ListParts([FromQuery] string? search)
    {
        var q = _db.Parts.AsNoTracking().Where(p => p.StockQuantity > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p =>
                p.PartName.ToLower().Contains(s) ||
                p.Category.ToLower().Contains(s) ||
                (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        var items = await q.OrderBy(p => p.PartName).ToListAsync();
        return Ok(items.Select(MapCatalogPart));
    }

    [HttpGet("parts/{id:guid}")]
    public async Task<IActionResult> GetPart(Guid id)
    {
        var part = await _db.Parts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (part is null)
            return NotFound(new { message = "Part not found." });
        if (part.StockQuantity <= 0)
            return NotFound(new { message = "This part is currently out of stock." });
        return Ok(MapCatalogPart(part));
    }

    [HttpPost("parts/checkout")]
    public async Task<IActionResult> CheckoutParts([FromBody] CustomerCheckoutDto dto)
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest(new { message = "Add at least one part to your order." });

        var customer = await _db.Users.FindAsync(userId.Value);
        if (customer is null)
            return NotFound(new { message = "Customer account not found." });

        var staffUser = await _db.Users
            .Where(u => u.Role == RoleType.Staff || u.Role == RoleType.Admin)
            .OrderBy(u => u.Role == RoleType.Staff ? 0 : 1)
            .FirstOrDefaultAsync();
        if (staffUser is null)
            return BadRequest(new { message = "Online checkout is not available right now. Please contact the centre." });

        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
            return BadRequest(new { message = "One or more parts were not found." });

        foreach (var item in dto.Items)
        {
            if (item.Quantity < 1)
                return BadRequest(new { message = "Quantity must be at least 1." });
            var part = parts.First(p => p.Id == item.PartId);
            if (part.StockQuantity < item.Quantity)
            {
                return Conflict(new
                {
                    message = $"Insufficient stock for '{part.PartName}'. Available: {part.StockQuantity}, requested: {item.Quantity}."
                });
            }
        }

        const decimal loyaltyThreshold = 5000m;
        const decimal loyaltyRate = 0.10m;

        var vehicleId = await ResolveCustomerVehicleIdAsync(userId.Value, dto.VehicleId);
        if (dto.VehicleId.HasValue && !vehicleId.HasValue)
            return BadRequest(new { message = "Vehicle not found on your account." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = new SalesInvoice
        {
            InvoiceNumber = await GenerateCustomerInvoiceNumberAsync(),
            CustomerId = userId.Value,
            VehicleId = vehicleId,
            StaffId = staffUser.Id,
            SaleDate = DateTime.UtcNow,
            PaymentStatus = MapCustomerPaymentStatus(dto.PaymentStatus),
        };

        decimal subtotal = 0m;
        decimal loyaltyDiscount = 0m;
        foreach (var item in dto.Items)
        {
            var part = parts.First(p => p.Id == item.PartId);
            var line = new SalesInvoiceItem
            {
                PartId = part.Id,
                Quantity = item.Quantity,
                UnitPrice = part.Price,
                SubTotal = part.Price * item.Quantity,
            };
            subtotal += line.SubTotal;
            if (line.SubTotal > loyaltyThreshold)
                loyaltyDiscount += Math.Round(line.SubTotal * loyaltyRate, 2);

            invoice.Items.Add(line);
            part.StockQuantity -= item.Quantity;
            part.UpdatedAt = DateTime.UtcNow;
        }

        invoice.DiscountApplied = loyaltyDiscount > 0;
        invoice.DiscountAmount = loyaltyDiscount;
        invoice.SubTotal = subtotal;
        invoice.TotalAmount = Math.Max(0m, subtotal - invoice.DiscountAmount);

        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _db.Entry(invoice).Reference(x => x.Customer).LoadAsync();
        if (invoice.VehicleId.HasValue)
            await _db.Entry(invoice).Reference(x => x.Vehicle).LoadAsync();
        foreach (var line in invoice.Items)
            await _db.Entry(line).Reference(x => x.Part).LoadAsync();

        await _notifications.NotifyUserAsync(
            userId.Value,
            $"Order {invoice.InvoiceNumber} confirmed — total Rs. {invoice.TotalAmount:N2}." +
            (loyaltyDiscount > 0 ? $" (includes Rs. {loyaltyDiscount:N2} loyalty savings)" : ""),
            NotificationType.General);

        await _notifications.NotifyAdminsAndStaffAsync(
            $"{customer.FullName} placed an online parts order {invoice.InvoiceNumber} for Rs. {invoice.TotalAmount:N2}.",
            NotificationType.General);

        return Ok(MapPurchase(invoice));
    }

    [HttpGet("purchases")]
    public async Task<IActionResult> GetPurchases()
    {
        var userId = GetUserId();
        if (userId == null) return UnauthorizedSession();

        var invoices = await _db.SalesInvoices
            .Include(s => s.Vehicle)
            .Include(s => s.Items)
            .ThenInclude(i => i.Part)
            .Where(s => s.CustomerId == userId.Value)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var reviewedInvoiceIds = await _db.Reviews
            .Where(r => r.CustomerId == userId.Value && r.RelatedInvoiceId != null)
            .Select(r => r.RelatedInvoiceId!.Value)
            .ToListAsync();
        var reviewedSet = reviewedInvoiceIds.ToHashSet();

        return Ok(invoices.Select(s => MapPurchase(s, reviewedSet.Contains(s.Id))));
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

    private async Task<Guid?> ResolveCustomerVehicleIdAsync(Guid customerId, Guid? requestedVehicleId)
    {
        if (requestedVehicleId.HasValue)
        {
            var exists = await _db.Vehicles.AsNoTracking()
                .AnyAsync(v => v.Id == requestedVehicleId.Value && v.CustomerId == customerId);
            return exists ? requestedVehicleId : null;
        }

        var vehicleIds = await _db.Vehicles.AsNoTracking()
            .Where(v => v.CustomerId == customerId)
            .Select(v => v.Id)
            .ToListAsync();
        return vehicleIds.Count == 1 ? vehicleIds[0] : null;
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

    private object MapAppointment(Appointment a) => new
    {
        id = a.Id.ToString(),
        service = a.ServiceType,
        date = a.PreferredDate.ToString("yyyy-MM-dd"),
        time = _scheduling.FormatTimeSlot(a.PreferredTime),
        status = MapAppointmentStatus(a.Status),
        statusRaw = a.Status.ToString(),
        vehicle = ExtractVehicle(a.Notes) ?? "—",
        amount = "—",
        staffNotes = a.StaffNotes,
        proposedDate = a.ProposedDate?.ToString("yyyy-MM-dd"),
        proposedTime = a.ProposedTime.HasValue ? _scheduling.FormatTimeSlot(a.ProposedTime.Value) : null,
        canAcceptReschedule = a.Status == AppointmentStatus.RescheduleProposed,
        canCancel = a.Status is AppointmentStatus.Pending or AppointmentStatus.Confirmed
            or AppointmentStatus.RescheduleProposed,
    };

    private async Task<string> GenerateCustomerInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        var todayCount = await _db.SalesInvoices.CountAsync(s => s.InvoiceNumber.StartsWith(prefix));
        return $"{prefix}{(todayCount + 1):D4}";
    }

    private static PaymentStatus MapCustomerPaymentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return PaymentStatus.Paid;
        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "paid" => PaymentStatus.Paid,
            "unpaid" or "pending" or "credit" => PaymentStatus.Unpaid,
            _ => PaymentStatus.Paid
        };
    }

    private static object MapCatalogPart(Part p) => new
    {
        id = p.Id.ToString(),
        name = p.PartName,
        partCode = string.IsNullOrWhiteSpace(p.Category) ? null : p.Category,
        description = string.IsNullOrWhiteSpace(p.Description) ? null : p.Description,
        price = p.Price,
        stockQuantity = p.StockQuantity,
        imageUrl = p.ImageUrl,
    };

    private static object MapPartRequest(PartRequest p) => new
    {
        id = p.Id.ToString(),
        partName = p.PartName,
        description = p.PartDescription,
        vehicle = p.VehicleModel,
        urgency = p.UrgencyLevel.ToString(),
        status = MapPartRequestStatus(p.Status),
        statusRaw = p.Status.ToString(),
        date = p.CreatedAt,
        estimatedAvailableDate = p.EstimatedAvailableDate?.ToString("yyyy-MM-dd"),
        staffNotes = p.StaffNotes,
        staffRespondedAt = p.StaffRespondedAt,
        escalatedAt = p.EscalatedAt,
        canDelete = p.Status == PartRequestStatus.Pending,
    };

    private static object MapReview(Review r)
    {
        var partsLabel = r.RelatedInvoice != null
            ? FormatInvoiceParts(r.RelatedInvoice)
            : null;
        return new
        {
            id = r.Id.ToString(),
            rating = r.Rating,
            title = r.Title,
            content = r.Description,
            service = MapReviewServiceLabel(r.ReviewCategory),
            date = r.CreatedAt,
            helpful = 0,
            invoiceId = r.RelatedInvoiceId?.ToString(),
            invoiceNumber = r.RelatedInvoice?.InvoiceNumber,
            items = partsLabel
        };
    }

    private static object MapNotification(Notification n) => new
    {
        id = n.Id.ToString(),
        title = MapNotificationTitle(n.Type),
        message = n.Message,
        type = MapNotificationTypeKey(n.Type),
        isRead = n.IsRead,
        createdAt = n.CreatedAt
    };

    private static object MapPurchase(SalesInvoice s, bool hasReview = false)
    {
        var itemNames = s.Items.Select(i => i.Part?.PartName ?? "Part").ToList();
        var itemsLabel = FormatInvoiceParts(s);

        var original = s.SubTotal + s.DiscountAmount;
        var discountPct = original > 0 && s.DiscountAmount > 0
            ? Math.Round(s.DiscountAmount / original * 100, 0)
            : (s.TotalAmount > 5000 ? 10m : 0m);

        return new
        {
            id = s.InvoiceNumber,
            invoiceId = s.Id.ToString(),
            date = s.SaleDate,
            vehicleId = s.VehicleId?.ToString(),
            vehicleNumber = VehicleDisplayHelper.FormatNumberPlate(s.Vehicle),
            vehicle = VehicleDisplayHelper.FormatFull(s.Vehicle),
            items = itemsLabel,
            parts = s.Items.Select(i => new
            {
                partId = i.PartId,
                partName = i.Part?.PartName ?? "Part",
                quantity = i.Quantity,
                unitPrice = i.UnitPrice,
                subTotal = i.SubTotal
            }).ToList(),
            originalAmount = original,
            discountAmount = s.DiscountAmount,
            discountPercentage = discountPct,
            amount = s.TotalAmount,
            status = s.PaymentStatus.ToString(),
            hasReview,
            canReview = !hasReview && s.Items.Any(i => i.PartId != Guid.Empty)
        };
    }

    private static string FormatInvoiceParts(SalesInvoice s)
    {
        var itemNames = s.Items.Select(i => i.Part?.PartName ?? "Part").ToList();
        if (itemNames.Count == 0) return "—";
        return string.Join(", ", itemNames.Take(3)) + (itemNames.Count > 3 ? $" +{itemNames.Count - 3} more" : "");
    }

    private static string MapAppointmentStatus(AppointmentStatus status) => status switch
    {
        AppointmentStatus.Pending => "Pending",
        AppointmentStatus.Confirmed => "Confirmed",
        AppointmentStatus.RescheduleProposed => "Reschedule Proposed",
        AppointmentStatus.Completed => "Completed",
        AppointmentStatus.Cancelled => "Cancelled",
        _ => status.ToString()
    };

    private static string MapPartRequestStatus(PartRequestStatus status) => status switch
    {
        PartRequestStatus.Pending => "Pending",
        PartRequestStatus.Estimated => "Available soon",
        PartRequestStatus.EscalatedToAdmin => "With admin",
        PartRequestStatus.Approved => "Ready to collect",
        PartRequestStatus.Rejected => "Rejected",
        PartRequestStatus.VendorRequested => "Ordered from vendor",
        PartRequestStatus.InvoiceRecorded => "Vendor invoice received",
        _ => status.ToString()
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
