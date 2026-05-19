using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Dtos.PartRequests;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class PartRequestAdminService : IPartRequestAdminService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _email;
    private readonly IPurchaseInvoiceService _purchaseInvoices;
    private readonly IUserNotificationService _notifications;

    public PartRequestAdminService(
        AppDbContext context,
        IEmailService email,
        IPurchaseInvoiceService purchaseInvoices,
        IUserNotificationService notifications)
    {
        _context = context;
        _email = email;
        _purchaseInvoices = purchaseInvoices;
        _notifications = notifications;
    }

    public async Task<PartRequest> RequestVendorAsync(Guid partRequestId, Guid adminUserId, RequestVendorForPartDto dto)
    {
        if (dto.VendorId == Guid.Empty)
            throw new ArgumentException("Select a vendor.");

        var request = await LoadPartRequestAsync(partRequestId);
        EnsureCanRequestVendor(request);

        var admin = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == RoleType.Admin)
            ?? throw new KeyNotFoundException("Admin account not found.");

        if (string.IsNullOrWhiteSpace(admin.BusinessEmail))
            throw new ArgumentException(
                "Set your business email in Admin profile before contacting vendors.");

        var vendor = await _context.Vendors.FindAsync(dto.VendorId)
            ?? throw new ArgumentException("Vendor not found.");

        if (string.IsNullOrWhiteSpace(vendor.Email))
            throw new ArgumentException("This vendor has no email address. Update the vendor record first.");

        var qty = dto.Quantity < 1 ? 1 : dto.Quantity;
        var customMessage = string.IsNullOrWhiteSpace(dto.Message) ? null : dto.Message.Trim();
        var subject = $"Part order request — {request.PartName} ({request.VehicleModel})";
        var body = BuildVendorEmailBody(request, vendor, admin, qty, customMessage);

        await _email.SendEmailAsync(
            vendor.Email.Trim(),
            subject,
            body,
            admin.BusinessEmail.Trim(),
            admin.FullName);

        request.VendorId = vendor.Id;
        request.VendorRequestedAt = DateTime.UtcNow;
        request.VendorRequestMessage = customMessage;
        request.Status = PartRequestStatus.VendorRequested;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            request.CustomerId,
            $"We have contacted our vendor about \"{request.PartName}\". We will update you when the supplier invoice is received.",
            NotificationType.General);

        return request;
    }

    public async Task<PartRequest> RecordVendorInvoiceAsync(
        Guid partRequestId,
        Guid adminUserId,
        RecordPartRequestVendorInvoiceDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.VendorInvoiceNumber))
            throw new ArgumentException("Enter the vendor invoice number from the supplier email.");

        if (dto.VendorId == Guid.Empty)
            throw new ArgumentException("Select a vendor.");

        if (dto.Quantity < 1)
            throw new ArgumentException("Quantity must be at least 1.");

        if (dto.UnitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.");

        var request = await LoadPartRequestAsync(partRequestId);
        EnsureCanRecordInvoice(request);

        var purchase = await _purchaseInvoices.CreateAsync(
            new CreatePurchaseInvoiceDto
            {
                VendorInvoiceNumber = dto.VendorInvoiceNumber.Trim(),
                VendorId = dto.VendorId,
                PurchaseDate = dto.PurchaseDate ?? DateTime.UtcNow,
                Items =
                [
                    new PurchaseInvoiceItemDto
                    {
                        ProductName = request.PartName,
                        Description = request.PartDescription,
                        Quantity = dto.Quantity,
                        UnitPrice = dto.UnitPrice,
                    },
                ],
            },
            adminUserId,
            allowAutoCreateVendor: true);

        request.VendorId = dto.VendorId;
        request.PurchaseInvoiceId = purchase.Id;
        request.InvoiceRecordedAt = DateTime.UtcNow;
        var invoiceNote = $"Vendor invoice {purchase.InvoiceNumber} recorded.";
        request.StaffNotes = string.IsNullOrWhiteSpace(request.StaffNotes)
            ? invoiceNote
            : $"{request.StaffNotes} | {invoiceNote}";
        request.Status = PartRequestStatus.InvoiceRecorded;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            request.CustomerId,
            $"Your part \"{request.PartName}\" has been ordered from the vendor (invoice {purchase.InvoiceNumber}). We will contact you when it is ready.",
            NotificationType.General);

        return request;
    }

    private async Task<PartRequest> LoadPartRequestAsync(Guid id)
    {
        return await _context.PartRequests
            .Include(p => p.Customer)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Part request not found.");
    }

    private static void EnsureCanRequestVendor(PartRequest request)
    {
        if (request.Status is PartRequestStatus.Rejected or PartRequestStatus.InvoiceRecorded)
            throw new ArgumentException("This request is closed.");

        if (request.Status is not (PartRequestStatus.EscalatedToAdmin or PartRequestStatus.Approved))
            throw new ArgumentException(
                "Only escalated or accepted requests can be sent to a vendor. Accept the request or wait for staff escalation.");
    }

    private static void EnsureCanRecordInvoice(PartRequest request)
    {
        if (request.Status == PartRequestStatus.Rejected)
            throw new ArgumentException("Rejected requests cannot receive a vendor invoice.");

        if (request.Status == PartRequestStatus.InvoiceRecorded)
            throw new ArgumentException("A vendor invoice is already recorded for this request.");

        if (request.Status != PartRequestStatus.VendorRequested)
            throw new ArgumentException(
                "Email the vendor first, then record the invoice when the supplier replies.");
    }

    private static string BuildVendorEmailBody(
        PartRequest request,
        Vendor vendor,
        User admin,
        int quantity,
        string? customMessage)
    {
        var notes = string.IsNullOrWhiteSpace(request.StaffNotes)
            ? "—"
            : System.Net.WebUtility.HtmlEncode(request.StaffNotes);

        var extra = string.IsNullOrWhiteSpace(customMessage)
            ? ""
            : $"<p><strong>Additional note:</strong> {System.Net.WebUtility.HtmlEncode(customMessage)}</p>";

        return $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; max-width: 640px;'>
  <p>Dear {System.Net.WebUtility.HtmlEncode(vendor.ContactPerson ?? vendor.VendorName)},</p>
  <p>We would like to order the following part for a customer. Please reply to this email with your quotation or invoice.</p>
  <table style='border-collapse: collapse; width: 100%; margin: 16px 0;' cellpadding='8'>
    <tr><td style='border:1px solid #ddd;'><strong>Part</strong></td><td style='border:1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(request.PartName)}</td></tr>
    <tr><td style='border:1px solid #ddd;'><strong>Description</strong></td><td style='border:1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(request.PartDescription)}</td></tr>
    <tr><td style='border:1px solid #ddd;'><strong>Vehicle</strong></td><td style='border:1px solid #ddd;'>{System.Net.WebUtility.HtmlEncode(request.VehicleModel)}</td></tr>
    <tr><td style='border:1px solid #ddd;'><strong>Quantity</strong></td><td style='border:1px solid #ddd;'>{quantity}</td></tr>
    <tr><td style='border:1px solid #ddd;'><strong>Urgency</strong></td><td style='border:1px solid #ddd;'>{request.UrgencyLevel}</td></tr>
    <tr><td style='border:1px solid #ddd;'><strong>Internal notes</strong></td><td style='border:1px solid #ddd;'>{notes}</td></tr>
  </table>
  {extra}
  <p>Please send your invoice to <strong>{System.Net.WebUtility.HtmlEncode(admin.BusinessEmail)}</strong>.</p>
  <p>Thank you,<br/>{System.Net.WebUtility.HtmlEncode(admin.FullName)}<br/>AutoPartsPro</p>
</body>
</html>";
    }
}
