using System.Security.Claims;

using Autopartspro.Application.Dtos.PartRequests;

using Autopartspro.Application.Interfaces;

using Autopartspro.Domain.Entities;

using Autopartspro.Domain.Enums;

using Autopartspro.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;



namespace Autopartspro.Controllers;



[ApiController]

[Route("api/part-requests")]

[Authorize(Roles = "Admin,Staff")]

public class PartRequestsController : ControllerBase

{

    private readonly AppDbContext _db;

    private readonly IUserNotificationService _notifications;

    private readonly IPartRequestAdminService _partRequestAdmin;



    public PartRequestsController(
        AppDbContext db,
        IUserNotificationService notifications,
        IPartRequestAdminService partRequestAdmin)

    {

        _db = db;

        _notifications = notifications;

        _partRequestAdmin = partRequestAdmin;

    }



    private bool IsAdmin => User.IsInRole("Admin");



    [HttpGet]

    public async Task<IActionResult> List([FromQuery] string? status, [FromQuery] bool escalatedOnly = false)

    {

        var q = _db.PartRequests

            .Include(p => p.Customer)

            .Include(p => p.Vendor)

            .Include(p => p.PurchaseInvoice)

            .AsNoTracking()

            .OrderByDescending(p => p.CreatedAt)

            .AsQueryable();



        if (escalatedOnly)

            q = q.Where(p => p.Status == PartRequestStatus.EscalatedToAdmin);

        else if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PartRequestStatus>(status, true, out var st))

            q = q.Where(p => p.Status == st);



        var list = await q.ToListAsync();

        return Ok(list.Select(MapStaffPartRequest));

    }



    [HttpPost("{id:guid}/escalate-to-admin")]

    [Authorize(Roles = "Staff")]

    public async Task<IActionResult> EscalateToAdmin(Guid id, [FromBody] EscalatePartRequestDto? dto)

    {

        var request = await _db.PartRequests

            .Include(p => p.Customer)

            .FirstOrDefaultAsync(p => p.Id == id);

        if (request == null)

            return NotFound(new { message = "Part request not found." });

        if (request.Status is PartRequestStatus.Approved or PartRequestStatus.Rejected)

            return BadRequest(new { message = "This request is already closed." });

        if (request.Status == PartRequestStatus.EscalatedToAdmin)

            return BadRequest(new { message = "This request is already with admin." });



        var note = string.IsNullOrWhiteSpace(dto?.Message)

            ? "Staff requested admin review for this part."

            : dto!.Message.Trim();



        request.Status = PartRequestStatus.EscalatedToAdmin;

        request.StaffNotes = note;

        request.StaffRespondedAt = DateTime.UtcNow;

        request.EscalatedAt = DateTime.UtcNow;

        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();



        var customerName = request.Customer?.FullName ?? "Customer";

        await _notifications.NotifyAdminsAsync(

            $"Part request escalated: \"{request.PartName}\" for {customerName} ({request.VehicleModel}). Note: {note}",

            NotificationType.General);



        await _notifications.NotifyUserAsync(

            request.CustomerId,

            $"Your part request for \"{request.PartName}\" has been referred to our admin team. We will update you soon.",

            NotificationType.General);



        return Ok(MapStaffPartRequest(request));

    }



    [HttpPost("{id:guid}/set-availability")]

    public async Task<IActionResult> SetAvailability(Guid id, [FromBody] SetPartAvailabilityDto dto)

    {

        var block = await LoadAndAuthorizeAsync(id);

        if (block.Error != null) return block.Error;

        var request = block.Request!;



        if (!DateOnly.TryParse(dto.Date, out var availableDate))

            return BadRequest(new { message = "Invalid availability date." });

        if (availableDate < DateOnly.FromDateTime(DateTime.UtcNow))

            return BadRequest(new { message = "Availability date cannot be in the past." });



        request.EstimatedAvailableDate = availableDate;

        request.StaffNotes = string.IsNullOrWhiteSpace(dto.Message)

            ? $"We expect your part to be available by {availableDate:yyyy-MM-dd}."

            : dto.Message.Trim();

        request.StaffRespondedAt = DateTime.UtcNow;

        request.Status = PartRequestStatus.Estimated;

        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();



        await _notifications.NotifyUserAsync(

            request.CustomerId,

            $"Part \"{request.PartName}\": expected available by {availableDate:yyyy-MM-dd}. {request.StaffNotes}",

            NotificationType.General);



        return Ok(MapStaffPartRequest(request));

    }



    [HttpPost("{id:guid}/approve")]

    public async Task<IActionResult> Approve(Guid id)

    {

        var block = await LoadAndAuthorizeAsync(id);

        if (block.Error != null) return block.Error;

        var request = block.Request!;



        if (request.Status == PartRequestStatus.Rejected)

            return BadRequest(new { message = "Rejected requests cannot be approved." });



        request.Status = PartRequestStatus.Approved;

        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();



        await _notifications.NotifyUserAsync(

            request.CustomerId,

            $"Your requested part \"{request.PartName}\" has been accepted and is ready. Visit us or contact the centre to collect.",

            NotificationType.General);



        return Ok(MapStaffPartRequest(request));

    }



    [HttpPost("{id:guid}/reject")]

    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectPartRequestDto? dto)

    {

        var block = await LoadAndAuthorizeAsync(id);

        if (block.Error != null) return block.Error;

        var request = block.Request!;



        request.Status = PartRequestStatus.Rejected;

        request.StaffNotes = string.IsNullOrWhiteSpace(dto?.Message)

            ? "We could not source this part."

            : dto!.Message.Trim();

        request.StaffRespondedAt = DateTime.UtcNow;

        request.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();



        await _notifications.NotifyUserAsync(

            request.CustomerId,

            $"Part request \"{request.PartName}\" was declined. {request.StaffNotes}",

            NotificationType.General);



        return Ok(MapStaffPartRequest(request));

    }



    [HttpPost("{id:guid}/request-vendor")]

    [Authorize(Roles = "Admin")]

    public async Task<IActionResult> RequestVendor(Guid id, [FromBody] RequestVendorForPartDto dto)

    {

        var adminId = GetUserId();

        if (adminId == null) return Unauthorized(new { message = "Invalid session." });

        try

        {

            var request = await _partRequestAdmin.RequestVendorAsync(id, adminId.Value, dto);

            return Ok(MapStaffPartRequest(request));

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



    [HttpPost("{id:guid}/record-vendor-invoice")]

    [Authorize(Roles = "Admin")]

    public async Task<IActionResult> RecordVendorInvoice(Guid id, [FromBody] RecordPartRequestVendorInvoiceDto dto)

    {

        var adminId = GetUserId();

        if (adminId == null) return Unauthorized(new { message = "Invalid session." });

        try

        {

            var request = await _partRequestAdmin.RecordVendorInvoiceAsync(id, adminId.Value, dto);

            return Ok(MapStaffPartRequest(request));

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

        return Guid.TryParse(claim, out var uid) ? uid : null;

    }



    private async Task<(PartRequest? Request, IActionResult? Error)> LoadAndAuthorizeAsync(Guid id)

    {

        var request = await _db.PartRequests

            .Include(p => p.Customer)

            .FirstOrDefaultAsync(p => p.Id == id);

        if (request == null)

            return (null, NotFound(new { message = "Part request not found." }));

        if (request.Status is PartRequestStatus.Rejected or PartRequestStatus.InvoiceRecorded)

            return (request, BadRequest(new { message = "This request is already closed." }));

        if (request.Status == PartRequestStatus.VendorRequested)

            return (request, BadRequest(new { message = "Vendor was contacted. Record the supplier invoice from Part requests." }));

        if (request.Status == PartRequestStatus.EscalatedToAdmin && !IsAdmin)

            return (request, BadRequest(new { message = "This request was sent to admin. Only an administrator can accept or reject it." }));



        return (request, null);

    }



    private object MapStaffPartRequest(PartRequest p) => new

    {

        id = p.Id.ToString(),

        customerId = p.CustomerId.ToString(),

        customerName = p.Customer?.FullName ?? "—",

        customerPhone = p.Customer?.PhoneNumber ?? "—",

        customerEmail = p.Customer?.Email ?? "—",

        partName = p.PartName,

        partDescription = p.PartDescription,

        vehicle = p.VehicleModel,

        urgency = p.UrgencyLevel.ToString(),

        status = p.Status.ToString(),

        statusLabel = MapStatusLabel(p.Status),

        estimatedAvailableDate = p.EstimatedAvailableDate?.ToString("yyyy-MM-dd"),

        staffNotes = p.StaffNotes,

        staffRespondedAt = p.StaffRespondedAt,

        escalatedAt = p.EscalatedAt,

        vendorId = p.VendorId?.ToString(),

        vendorName = p.Vendor?.VendorName,

        vendorRequestedAt = p.VendorRequestedAt,

        purchaseInvoiceId = p.PurchaseInvoiceId?.ToString(),

        linkedInvoiceNumber = p.PurchaseInvoice?.InvoiceNumber,

        invoiceRecordedAt = p.InvoiceRecordedAt,

        createdAt = p.CreatedAt,

        canStaffAct = p.Status is PartRequestStatus.Pending or PartRequestStatus.Estimated,

        canAdminAct = p.Status is PartRequestStatus.EscalatedToAdmin or PartRequestStatus.Approved,

        canEscalate = p.Status is PartRequestStatus.Pending or PartRequestStatus.Estimated,

        canRequestVendor = IsAdmin && p.Status is PartRequestStatus.EscalatedToAdmin or PartRequestStatus.Approved,

        canRecordVendorInvoice = IsAdmin && p.Status == PartRequestStatus.VendorRequested,

    };



    private static string MapStatusLabel(PartRequestStatus status) => status switch

    {

        PartRequestStatus.Pending => "Pending",

        PartRequestStatus.Estimated => "Availability set",

        PartRequestStatus.EscalatedToAdmin => "With admin",

        PartRequestStatus.Approved => "Accepted",

        PartRequestStatus.Rejected => "Rejected",

        PartRequestStatus.VendorRequested => "Vendor contacted",

        PartRequestStatus.InvoiceRecorded => "Invoice recorded",

        _ => status.ToString()

    };

}


