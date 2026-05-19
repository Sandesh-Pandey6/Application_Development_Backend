using Autopartspro.Application.Dtos;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Entities;
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
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif",
    };

    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IUserNotificationService _notifications;

    public PartsController(AppDbContext db, IWebHostEnvironment env, IUserNotificationService notifications)
    {
        _db = db;
        _env = env;
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
    [RequestSizeLimit(MaxImageBytes)]
    public async Task<ActionResult<PartDto>> UploadImage(Guid id, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Choose an image file to upload." });

        if (file.Length > MaxImageBytes)
            return BadRequest(new { message = "Image must be 5 MB or smaller." });

        if (!AllowedImageTypes.Contains(file.ContentType))
            return BadRequest(new { message = "Use JPEG, PNG, WebP, or GIF." });

        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 6)
            ext = file.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg",
            };

        var uploadsDir = GetUploadsDirectory();
        Directory.CreateDirectory(uploadsDir);

        await DeleteStoredImageAsync(p.ImageUrl);

        var fileName = $"{id:N}{ext.ToLowerInvariant()}";
        var physicalPath = Path.Combine(uploadsDir, fileName);
        await using (var stream = new FileStream(physicalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        p.ImageUrl = $"/uploads/parts/{fileName}";
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return MapPart(p);
    }

    [HttpDelete("{id:guid}/image")]
    public async Task<ActionResult<PartDto>> RemoveImage(Guid id)
    {
        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();

        await DeleteStoredImageAsync(p.ImageUrl);
        p.ImageUrl = null;
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

        await DeleteStoredImageAsync(p.ImageUrl);
        _db.Parts.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private string GetUploadsDirectory()
    {
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(_env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "parts");
    }

    private async Task DeleteStoredImageAsync(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var fileName = Path.GetFileName(imageUrl);
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        var path = Path.Combine(GetUploadsDirectory(), fileName);
        if (System.IO.File.Exists(path))
        {
            try
            {
                await Task.Run(() => System.IO.File.Delete(path));
            }
            catch
            {
                // ignore cleanup failures
            }
        }
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
