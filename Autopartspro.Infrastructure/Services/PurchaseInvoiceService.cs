using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly AppDbContext _context;
    private readonly IUserNotificationService _notifications;

    public PurchaseInvoiceService(AppDbContext context, IUserNotificationService notifications)
    {
        _context = context;
        _notifications = notifications;
    }

    public async Task<PurchaseInvoiceListResponseDto> GetAllAsync()
    {
        var invoices = await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync();

        var invoiceDtos = invoices.Select(MapListItem).ToList();

        return new PurchaseInvoiceListResponseDto
        {
            TotalInvoices = invoiceDtos.Count,
            TotalValue = invoiceDtos.Sum(i => i.TotalAmount),
            Completed = invoiceDtos.Count(i => i.Status == nameof(PurchaseInvoiceStatus.Completed)),
            PendingOrProcessing = invoiceDtos.Count(i =>
                i.Status is nameof(PurchaseInvoiceStatus.Pending) or nameof(PurchaseInvoiceStatus.Processing)),
            Invoices = invoiceDtos,
        };
    }

    public async Task<PurchaseInvoiceResponseDto> GetByIdAsync(Guid id)
    {
        var invoice = await LoadInvoiceAsync(id);
        return MapDetail(invoice);
    }

    public async Task<PurchaseInvoiceResponseDto> CreateAsync(
        CreatePurchaseInvoiceDto dto,
        Guid recordedByUserId,
        bool allowAutoCreateVendor = false)
    {
        if (dto.Items is null || dto.Items.Count == 0)
            throw new ArgumentException("Add at least one product to the purchase.");

        var recorderExists = await _context.Users.AnyAsync(u => u.Id == recordedByUserId);
        if (!recorderExists)
            throw new ArgumentException("Your session is invalid. Please log in again.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var vendor = await ResolveVendorAsync(dto, allowAutoCreateVendor);

            var invoiceNumber = await ResolveInvoiceNumberAsync(dto.VendorInvoiceNumber);

            decimal totalAmount = 0;
            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = invoiceNumber,
                VendorId = vendor.Id,
                AdminId = recordedByUserId,
                PurchaseDate = dto.PurchaseDate == default
                    ? DateTime.UtcNow
                    : dto.PurchaseDate.ToUniversalTime(),
                Status = PurchaseInvoiceStatus.Completed,
            };

            foreach (var item in dto.Items)
            {
                if (item.Quantity <= 0)
                    throw new ArgumentException("Quantity must be at least 1.");

                if (item.UnitPrice < 0)
                    throw new ArgumentException("Unit price cannot be negative.");

                var part = await ResolvePartAsync(vendor.Id, item);
                var subTotal = item.Quantity * item.UnitPrice;
                totalAmount += subTotal;

                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    SubTotal = subTotal,
                });

                part.StockQuantity += item.Quantity;
                part.UpdatedAt = DateTime.UtcNow;
            }

            invoice.TotalAmount = totalAmount;
            _context.PurchaseInvoices.Add(invoice);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var vendorName = vendor.VendorName;
            await _notifications.NotifyAdminsAsync(
                $"Vendor purchase {invoiceNumber} recorded from {vendorName} — Rs. {totalAmount:N2} ({invoice.Items.Sum(i => i.Quantity)} items).",
                NotificationType.General);

            return await GetByIdAsync(invoice.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private async Task<string> ResolveInvoiceNumberAsync(string? vendorInvoiceNumber)
    {
        var provided = vendorInvoiceNumber?.Trim();
        if (!string.IsNullOrWhiteSpace(provided))
        {
            if (provided.Length > 64)
                throw new ArgumentException("Vendor invoice number must be 64 characters or less.");

            var exists = await _context.PurchaseInvoices
                .AnyAsync(p => p.InvoiceNumber == provided);
            if (exists)
                throw new ArgumentException("This vendor invoice number is already saved.");

            return provided;
        }

        var prefix = $"PI-{DateTime.UtcNow:yyyyMMdd}-";
        var todayCount = await _context.PurchaseInvoices
            .CountAsync(p => p.InvoiceNumber.StartsWith(prefix));
        return $"{prefix}{(todayCount + 1):D4}";
    }

    private async Task<Vendor> ResolveVendorAsync(CreatePurchaseInvoiceDto dto, bool allowAutoCreateVendor)
    {
        if (dto.VendorId is { } vendorId && vendorId != Guid.Empty)
        {
            return await _context.Vendors.FindAsync(vendorId)
                ?? throw new ArgumentException("Vendor not found. Choose a vendor added by an administrator.");
        }

        if (!allowAutoCreateVendor)
            throw new ArgumentException(
                "Select a vendor from the list. Only administrators can add new vendors.");

        var name = dto.VendorName?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vendor name is required.");

        var normalized = name.ToLowerInvariant();
        var vendor = await _context.Vendors
            .FirstOrDefaultAsync(v => v.VendorName.ToLower() == normalized);

        if (vendor is null)
        {
            vendor = new Vendor
            {
                VendorName = name,
                Address = dto.VendorAddress?.Trim() ?? string.Empty,
                PhoneNumber = dto.VendorPhone?.Trim() ?? string.Empty,
            };
            _context.Vendors.Add(vendor);
            return vendor;
        }

        if (!string.IsNullOrWhiteSpace(dto.VendorAddress))
            vendor.Address = dto.VendorAddress.Trim();
        if (!string.IsNullOrWhiteSpace(dto.VendorPhone))
            vendor.PhoneNumber = dto.VendorPhone.Trim();
        vendor.UpdatedAt = DateTime.UtcNow;
        return vendor;
    }

    private async Task<Part> ResolvePartAsync(Guid vendorId, PurchaseInvoiceItemDto item)
    {
        if (item.PartId is { } partId && partId != Guid.Empty)
        {
            return await _context.Parts.FindAsync(partId)
                ?? throw new ArgumentException($"Part not found: {partId}");
        }

        var productName = item.ProductName?.Trim();
        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Each line needs a product name.");

        var normalized = productName.ToLowerInvariant();
        var part = await _context.Parts
            .FirstOrDefaultAsync(p =>
                p.VendorId == vendorId && p.PartName.ToLower() == normalized);

        if (part is not null)
            return part;

        part = new Part
        {
            PartName = productName,
            VendorId = vendorId,
            Price = item.UnitPrice,
            StockQuantity = 0,
            Category = string.Empty,
            Description = string.Empty,
        };
        _context.Parts.Add(part);
        return part;
    }

    public async Task<byte[]> GetPdfAsync(Guid id)
    {
        var invoice = await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items)
            .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Purchase invoice not found.");

        return PurchaseInvoicePdfGenerator.Build(invoice);
    }

    private async Task<PurchaseInvoice> LoadInvoiceAsync(Guid id)
    {
        return await _context.PurchaseInvoices
            .AsNoTracking()
            .Include(p => p.Vendor)
            .Include(p => p.Items)
            .ThenInclude(i => i.Part)
            .FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new KeyNotFoundException("Purchase invoice not found.");
    }

    private static PurchaseInvoiceResponseDto MapListItem(PurchaseInvoice p) => new()
    {
        Id = p.Id,
        InvoiceNumber = p.InvoiceNumber,
        VendorName = p.Vendor.VendorName,
        VendorId = p.VendorId,
        TotalItems = p.Items.Sum(i => i.Quantity),
        TotalAmount = p.TotalAmount,
        PurchaseDate = p.PurchaseDate,
        Status = p.Status.ToString(),
    };

    private static PurchaseInvoiceResponseDto MapDetail(PurchaseInvoice invoice) => new()
    {
        Id = invoice.Id,
        InvoiceNumber = invoice.InvoiceNumber,
        VendorName = invoice.Vendor.VendorName,
        VendorId = invoice.VendorId,
        TotalItems = invoice.Items.Sum(i => i.Quantity),
        TotalAmount = invoice.TotalAmount,
        PurchaseDate = invoice.PurchaseDate,
        Status = invoice.Status.ToString(),
        Items = invoice.Items.Select(i => new PurchaseInvoiceItemResponseDto
        {
            PartName = i.Part?.PartName ?? "Part",
            Category = i.Part?.Category ?? "",
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            SubTotal = i.SubTotal,
        }).ToList(),
    };
}
