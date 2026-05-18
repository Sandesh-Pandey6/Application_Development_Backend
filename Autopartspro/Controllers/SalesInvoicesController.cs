using Autopartspro.Application.Dtos;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/invoices")]
public class SalesInvoicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public SalesInvoicesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> List(
        [FromQuery] Guid? customerId,
        [FromQuery] string? search)
    {
        var q = _db.SalesInvoices.AsNoTracking()
            .Include(s => s.Customer)
            .Include(s => s.Items)
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
            .FirstOrDefaultAsync(s => s.Id == id);
        if (s is null) return NotFound();
        return MapInvoice(s);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create([FromBody] InvoiceCreateDto dto)
    {
        if (dto.Items.Count == 0)
        {
            return BadRequest(new { message = "At least one item is required." });
        }

        var customer = await _db.Users.FindAsync(dto.CustomerId);
        if (customer is null) return BadRequest(new { message = "Customer not found." });

        if (!Enum.TryParse<PaymentStatus>(dto.PaymentStatus, true, out var paymentStatus))
        {
            paymentStatus = PaymentStatus.Paid;
        }

        var partIds = dto.Items.Select(i => i.PartId).Distinct().ToList();
        var parts = await _db.Parts.Where(p => partIds.Contains(p.Id)).ToListAsync();
        if (parts.Count != partIds.Count)
        {
            return BadRequest(new { message = "One or more parts not found." });
        }

        foreach (var item in dto.Items)
        {
            var part = parts.First(p => p.Id == item.PartId);
            if (part.StockQuantity < item.Quantity)
            {
                return Conflict(new { message = $"Insufficient stock for '{part.PartName}'. Available: {part.StockQuantity}, requested: {item.Quantity}." });
            }
        }

        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = new SalesInvoice
        {
            InvoiceNumber = await GenerateInvoiceNumberAsync(),
            CustomerId = dto.CustomerId,
            StaffId = dto.StaffId ?? Guid.Empty, // Or a default staff
            SaleDate = DateTime.UtcNow,
            DiscountAmount = dto.DiscountAmount,
            DiscountApplied = dto.DiscountAmount > 0,
            PaymentStatus = paymentStatus,
        };

        decimal subtotal = 0m;
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
            invoice.Items.Add(line);

            part.StockQuantity -= item.Quantity;
            part.UpdatedAt = DateTime.UtcNow;
        }

        invoice.SubTotal = subtotal;
        invoice.TotalAmount = Math.Max(0m, subtotal - dto.DiscountAmount);

        _db.SalesInvoices.Add(invoice);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _db.Entry(invoice).Reference(x => x.Customer).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = invoice.Id }, MapInvoice(invoice));
    }

    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var prefix = $"INV-{DateTime.UtcNow:yyyyMMdd}-";
        var todayCount = await _db.SalesInvoices.CountAsync(s => s.InvoiceNumber.StartsWith(prefix));
        return $"{prefix}{(todayCount + 1):D4}";
    }

    private static InvoiceDto MapInvoice(SalesInvoice s) => new(
        s.Id,
        s.InvoiceNumber,
        s.CustomerId,
        s.Customer?.FullName ?? string.Empty,
        s.StaffId,
        s.Staff?.FullName ?? string.Empty,
        s.SaleDate,
        s.SubTotal,
        s.DiscountAmount,
        s.TotalAmount,
        s.PaymentStatus.ToString(),
        s.Items.Select(i => new InvoiceItemDto(
            i.Id, i.PartId, i.Part?.PartName, i.Quantity, i.UnitPrice, i.SubTotal)).ToList());
}
