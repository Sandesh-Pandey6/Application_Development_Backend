using Autopartspro.Application.Dtos;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
using Autopartspro.Infrastructure;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/parts")]
[Authorize(Roles = "Admin,Staff")]
public class PartsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IImageStorageService _images;
    private readonly IUserNotificationService _notifications;

    public PartsController(
        AppDbContext db,
        IImageStorageService images,
        IUserNotificationService notifications)
    {
        _db = db;
        _images = images;
        _notifications = notifications;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PartDto>>> List(
        [FromQuery] string? search,
        [FromQuery] bool? inStock)
    {
        var q = _db.Parts.AsNoTracking().Include(p => p.Vendor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(p =>
                p.PartName.ToLower().Contains(s) ||
                p.Category.ToLower().Contains(s) ||
                (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (inStock == true)
            q = q.Where(p => p.StockQuantity > 0);

        var items = await q.OrderBy(p => p.PartName).ToListAsync();
        return Ok(items.Select(MapPart));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PartDto>> Get(Guid id)
    {
        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return MapPart(p);
    }

    [HttpPost]
    public async Task<ActionResult<PartDto>> Create([FromBody] PartUpsertDto dto)
    {
        if (dto.VendorId.HasValue && !await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value))
            return BadRequest(new { message = "Vendor not found." });

        var p = new Part
        {
            PartName = dto.Name.Trim(),
            Category = dto.PartCode?.Trim() ?? string.Empty,
            Description = dto.Description?.Trim() ?? string.Empty,
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            ImageUrl = NormalizeImageUrl(dto.ImageUrl),
            VendorId = dto.VendorId ?? Guid.Empty,
        };

        if (p.VendorId == Guid.Empty)
        {
            var firstVendor = await _db.Vendors.OrderBy(v => v.VendorName).FirstOrDefaultAsync();
            if (firstVendor is null)
                return BadRequest(new { message = "No vendor exists. Add a vendor first (admin) or record a vendor purchase." });
            p.VendorId = firstVendor.Id;
        }

        _db.Parts.Add(p);
        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();

        if (p.StockQuantity <= 10)
            await _notifications.NotifyLowStockAsync(p.PartName, p.StockQuantity);

        return CreatedAtAction(nameof(Get), new { id = p.Id }, MapPart(p));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PartDto>> Update(Guid id, [FromBody] PartUpsertDto dto)
    {
        var p = await _db.Parts.FindAsync(id);
        if (p is null) return NotFound();

        if (dto.VendorId.HasValue && !await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value))
            return BadRequest(new { message = "Vendor not found." });

        p.PartName = dto.Name.Trim();
        p.Category = dto.PartCode?.Trim() ?? string.Empty;
        p.Description = dto.Description?.Trim() ?? string.Empty;
        p.Price = dto.Price;
        p.StockQuantity = dto.StockQuantity;
        if (dto.VendorId.HasValue) p.VendorId = dto.VendorId.Value;
        if (dto.ImageUrl is not null)
            p.ImageUrl = NormalizeImageUrl(dto.ImageUrl);
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();

        if (p.StockQuantity <= 10)
            await _notifications.NotifyLowStockAsync(p.PartName, p.StockQuantity);

        return MapPart(p);
    }

    [HttpPost("{id:guid}/image")]
    [RequestSizeLimit(ImageUploadRules.MaxBytes)]
    public async Task<ActionResult<PartDto>> UploadImage(Guid id, IFormFile? file)
    {
        try
        {
            ImageUploadRules.Validate(file);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        await _images.DeleteAsync(p.ImageUrl, p.ImagePublicId);

        await using var stream = file!.OpenReadStream();
        var upload = await _images.UploadAsync(new ImageUploadRequest(
            stream,
            file.FileName,
            file.ContentType,
            "parts",
            id.ToString("N")));

        p.ImageUrl = upload.Url;
        p.ImagePublicId = upload.PublicId;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapPart(p);
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<PartDto>> RemoveImage(Guid id)
    {
        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        await _images.DeleteAsync(p.ImageUrl, p.ImagePublicId);
        p.ImageUrl = null;
        p.ImagePublicId = null;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapPart(p);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Parts.FindAsync(id);
        if (p is null) return NotFound();

        var sold = await _db.SalesInvoiceItems.AnyAsync(i => i.PartId == id);
        if (sold)
            return Conflict(new { message = "Part has been sold and cannot be deleted." });

        await _images.DeleteAsync(p.ImageUrl, p.ImagePublicId);
        _db.Parts.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static string? NormalizeImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var trimmed = url.Trim();
        return trimmed.Length > 500 ? trimmed[..500] : trimmed;
    }

    private static PartDto MapPart(Part p) => new(
        p.Id,
        p.PartName,
        string.IsNullOrWhiteSpace(p.Category) ? null : p.Category,
        string.IsNullOrWhiteSpace(p.Description) ? null : p.Description,
        p.Price,
        p.StockQuantity,
        p.VendorId,
        p.Vendor?.VendorName,
        p.ImageUrl);
}
