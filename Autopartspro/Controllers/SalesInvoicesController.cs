using System.Security.Claims;
using Autopartspro.Application.Dtos;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = "Admin,Staff")]
public class SalesInvoicesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISalesInvoiceService _salesInvoiceService;
    private readonly IUserNotificationService _notifications;

    public SalesInvoicesController(
        AppDbContext db,
        ISalesInvoiceService salesInvoiceService,
        IUserNotificationService notifications)
    {
        _db = db;
        _salesInvoiceService = salesInvoiceService;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> List(
        [FromQuery] Guid? customerId,
        [FromQuery] string? search)
    {
        var q = _db.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Staff)
            .Include(s => s.Items)
            .ThenInclude(i => i.Part)
            .AsQueryable();

        if (customerId.HasValue) q = q.Where(s => s.CustomerId == customerId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var t = search.Trim().ToLower();
            q = q.Where(s =>
                s.InvoiceNumber.ToLower().Contains(t) ||
                (s.Customer != null && s.Customer.FullName.ToLower().Contains(t)));
        }

        var items = await q.OrderByDescending(s => s.SaleDate)
            .Select(s => MapInvoice(s))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> Get(Guid id)
    {
        var s = await _db.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Staff)
            .Include(s => s.Items)
            .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (s is null) return NotFound();
        return MapInvoice(s);
    }

    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DownloadPdf(Guid id)
    {
        try
        {
            var pdf = await _salesInvoiceService.GetPaidInvoicePdfAsync(id);
            var invoice = await _db.SalesInvoices.AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new { s.InvoiceNumber })
                .FirstOrDefaultAsync();
            if (invoice is null) return NotFound(new { message = "Invoice not found." });
            return File(pdf, "application/pdf", $"{invoice.InvoiceNumber}.pdf");
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = "Invoice not found." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] InvoiceCreateDto dto)
    {
        if (dto.Items.Count == 0)
            return BadRequest(new { message = "At least one item is required." });

        var customer = await _db.Users.FindAsync(dto.CustomerId);
        if (customer is null) return BadRequest(new { message = "Customer not found." });

        var paymentStatus = MapPaymentStatus(dto.PaymentStatus);

        var staffUser = await ResolveStaffUserAsync();
        if (staffUser is null)
            return BadRequest(new { message = "Could not identify the staff member recording this sale." });

        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
            return BadRequest(new { message = "One or more parts not found." });

        foreach (var item in dto.Items)
        {
            var part = parts.First(p => p.Id == item.PartId);
            if (part.StockQuantity < item.Quantity)
            {
                return Conflict(new
                {
                    message = $"Insufficient stock for '{part.PartName}'. Available: {part.StockQuantity}, requested: {item.Quantity}."
                });
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        const decimal loyaltyThreshold = 5000m;
        const decimal loyaltyRate = 0.10m;

        var invoice = new SalesInvoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = dto.CustomerId,
            StaffId = staffUser!.Id,
            SaleDate = DateTime.UtcNow,
            PaymentStatus = paymentStatus,
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

        var manualDiscount = Math.Max(0m, dto.DiscountAmount);
        invoice.DiscountApplied = loyaltyDiscount > 0;
        invoice.DiscountAmount = loyaltyDiscount + manualDiscount;
        invoice.SubTotal = subtotal;
        invoice.TotalAmount = Math.Max(0m, subtotal - invoice.DiscountAmount);

        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _db.Entry(invoice).Reference(x => x.Customer).LoadAsync();
        await _db.Entry(invoice).Reference(x => x.Staff).LoadAsync();
        foreach (var line in invoice.Items)
            await _db.Entry(line).Reference(x => x.Part).LoadAsync();

        if (paymentStatus == PaymentStatus.Unpaid && invoice.Customer is not null)
        {
            await _notifications.NotifyUserAsync(
                invoice.CustomerId,
                $"Invoice {invoice.InvoiceNumber} for Rs. {invoice.TotalAmount:N2} is on credit. Please arrange payment when convenient.",
                NotificationType.CreditReminder);
        }

        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, MapInvoice(invoice));
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        var todayCount = await _db.SalesInvoices.CountAsync(s => s.InvoiceNumber.StartsWith(prefix));
        return $"{prefix}{(todayCount + 1):D4}";
    }

    private async Task<User?> ResolveStaffUserAsync()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(idClaim, out var userId))
        {
            var current = await _db.Users.FindAsync(userId);
            if (current is { Role: RoleType.Staff or RoleType.Admin })
                return current;
        }

        return await _db.Users
            .Where(u => u.Role == RoleType.Staff || u.Role == RoleType.Admin)
            .OrderBy(u => u.Role == RoleType.Staff ? 0 : 1)
            .FirstOrDefaultAsync();
    }

    private static PaymentStatus MapPaymentStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return PaymentStatus.Paid;

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "paid" => PaymentStatus.Paid,
            "unpaid" or "pending" or "credit" or "overdue" => PaymentStatus.Unpaid,
            _ => Enum.TryParse<PaymentStatus>(status, true, out var parsed)
                ? parsed
                : PaymentStatus.Paid
        };
    }

    private static InvoiceDto MapInvoice(SalesInvoice s) => new(
        s.Id,
        s.InvoiceNumber,
        s.CustomerId,
        s.Customer?.FullName ?? string.Empty,
        string.IsNullOrWhiteSpace(s.Staff?.FullName) ? null : s.Staff.FullName,
        s.SaleDate,
        s.SubTotal,
        s.DiscountAmount,
        s.TotalAmount,
        s.PaymentStatus.ToString(),
        s.Items.Select(i => new InvoiceItemDto(
            i.Id, i.PartId, i.Part?.PartName, i.Quantity, i.UnitPrice, i.SubTotal)).ToList());
}
