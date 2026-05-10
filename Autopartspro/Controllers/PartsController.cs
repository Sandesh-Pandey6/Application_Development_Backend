using Autopartspro.Application.Dtos;
using Autopartspro.Domain.Entities;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/parts")]
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
                p.Name.ToLower().Contains(s) ||
                (p.PartCode != null && p.PartCode.ToLower().Contains(s)) ||
                (p.Description != null && p.Description.ToLower().Contains(s)));
        }

        if (inStock == true)
        {
            q = q.Where(p => p.StockQuantity > 0);
        }

        var items = await q.OrderBy(p => p.Name)
            .Select(p => new PartDto(
                p.Id, p.Name, p.PartCode, p.Description, p.Price, p.StockQuantity,
                p.VendorId, p.Vendor != null ? p.Vendor.Name : null))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PartDto>> Get(int id)
    {
        var p = await _db.Parts.Include(x => x.Vendor).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        return new PartDto(p.Id, p.Name, p.PartCode, p.Description, p.Price, p.StockQuantity,
            p.VendorId, p.Vendor?.Name);
    }

    [HttpPost]
    public async Task<ActionResult<PartDto>> Create([FromBody] PartUpsertDto dto)
    {
        if (dto.VendorId.HasValue && !await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value))
        {
            return BadRequest(new { message = "Vendor not found." });
        }

        var p = new Part
        {
            Name = dto.Name.Trim(),
            PartCode = dto.PartCode?.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            VendorId = dto.VendorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Parts.Add(p);
        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();

        var result = new PartDto(p.Id, p.Name, p.PartCode, p.Description, p.Price, p.StockQuantity,
            p.VendorId, p.Vendor?.Name);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PartDto>> Update(int id, [FromBody] PartUpsertDto dto)
    {
        var p = await _db.Parts.FindAsync(id);
        if (p is null) return NotFound();

        if (dto.VendorId.HasValue && !await _db.Vendors.AnyAsync(v => v.Id == dto.VendorId.Value))
        {
            return BadRequest(new { message = "Vendor not found." });
        }

        p.Name = dto.Name.Trim();
        p.PartCode = dto.PartCode?.Trim();
        p.Description = dto.Description?.Trim();
        p.Price = dto.Price;
        p.StockQuantity = dto.StockQuantity;
        p.VendorId = dto.VendorId;
        p.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        await _db.Entry(p).Reference(x => x.Vendor).LoadAsync();
        return new PartDto(p.Id, p.Name, p.PartCode, p.Description, p.Price, p.StockQuantity,
            p.VendorId, p.Vendor?.Name);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Parts.FindAsync(id);
        if (p is null) return NotFound();
        var sold = await _db.SalesInvoiceItems.AnyAsync(i => i.PartId == id);
        if (sold)
        {
            return Conflict(new { message = "Part has been sold and cannot be deleted." });
        }
        _db.Parts.Remove(p);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
