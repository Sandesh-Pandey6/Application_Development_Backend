using Autopartspro.Application.Dtos;
using Autopartspro.Domain.Entities;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly AppDbContext _db;

    public VendorsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorDto>>> List([FromQuery] string? search)
    {
        var q = _db.Vendors.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(v =>
                v.VendorName.ToLower().Contains(s) ||
                (v.ContactPerson != null && v.ContactPerson.ToLower().Contains(s)) ||
                (v.Email != null && v.Email.ToLower().Contains(s)) ||
                (v.PhoneNumber != null && v.PhoneNumber.Contains(s)));
        }
        var items = await q.OrderBy(v => v.VendorName)
            .Select(v => new VendorDto(v.Id, v.VendorName, v.ContactPerson, v.Email, v.PhoneNumber, v.Address, v.CreatedAt, v.UpdatedAt))
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VendorDto>> Get(Guid id)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v is null) return NotFound();
        return new VendorDto(v.Id, v.VendorName, v.ContactPerson, v.Email, v.PhoneNumber, v.Address, v.CreatedAt, v.UpdatedAt);
    }

    [HttpPost]
    public async Task<ActionResult<VendorDto>> Create([FromBody] VendorUpsertDto dto)
    {
        var v = new Vendor
        {
            VendorName = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson?.Trim(),
            Email = dto.Email?.Trim(),
            PhoneNumber = dto.Phone?.Trim(),
            Address = dto.Address?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.Vendors.Add(v);
        await _db.SaveChangesAsync();
        var result = new VendorDto(v.Id, v.VendorName, v.ContactPerson, v.Email, v.PhoneNumber, v.Address, v.CreatedAt, v.UpdatedAt);
        return CreatedAtAction(nameof(Get), new { id = v.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VendorDto>> Update(Guid id, [FromBody] VendorUpsertDto dto)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v is null) return NotFound();
        v.VendorName = dto.Name.Trim();
        v.ContactPerson = dto.ContactPerson?.Trim();
        v.Email = dto.Email?.Trim();
        v.PhoneNumber = dto.Phone?.Trim();
        v.Address = dto.Address?.Trim();
        v.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new VendorDto(v.Id, v.VendorName, v.ContactPerson, v.Email, v.PhoneNumber, v.Address, v.CreatedAt, v.UpdatedAt);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v is null) return NotFound();

        var hasParts = await _db.Parts.AnyAsync(p => p.VendorId == id);
        if (hasParts)
        {
            return Conflict(new { message = "Vendor has parts associated. Reassign or delete those parts first." });
        }

        _db.Vendors.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
