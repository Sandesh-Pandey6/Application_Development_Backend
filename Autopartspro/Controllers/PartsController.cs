using Autopartspro.Application.Dtos;
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
    private readonly AppDbContext _db;

    public PartsController(AppDbContext db) => _db = db;

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

        var items = await q.OrderBy(p => p.PartName)
            .Select(p => new PartDto(
                p.Id, p.PartName, p.Category, p.Description, p.Price, p.StockQuantity,
                null, p.Vendor != null ? p.Vendor.VendorName : null))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PartDto>> Get(Guid id)
    {
        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return new PartDto(p.Id, p.PartName, p.Category, p.Description, p.Price, p.StockQuantity,
            null, p.Vendor?.VendorName);
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
            VendorId = dto.VendorId ?? Guid.Empty,
        };

        if (p.VendorId == Guid.Empty)
        {
            var firstVendor = await _db.Vendors.OrderBy(v => v.VendorName).FirstOrDefaultAsync();
            if (firstVendor is null)
                return BadRequest(new { message = "No vendor exists. Create a vendor first." });
            p.VendorId = firstVendor.Id;
        }

        _db.Parts.Add(p);
        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();

        return CreatedAtAction(nameof(Get), new { id = p.Id },
            new PartDto(p.Id, p.PartName, p.Category, p.Description, p.Price, p.StockQuantity,
                null, p.Vendor?.VendorName));
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
        p.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();

        return new PartDto(p.Id, p.PartName, p.Category, p.Description, p.Price, p.StockQuantity,
            null, p.Vendor?.VendorName);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var p = await _db.Parts.FindAsync(id);
        if (p is null) return NotFound();

        var sold = await _db.SalesInvoiceItems.AnyAsync(i => i.PartId == id);
        if (sold)
            return Conflict(new { message = "Part has been sold and cannot be deleted." });

        _db.Parts.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
