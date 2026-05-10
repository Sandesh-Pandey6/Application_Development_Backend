using Autopartspro.Application.Dtos;
using Autopartspro.Domain.Entities;
using Autopartspro.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _db;

    public CustomersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? vehicleNumber)
    {
        var q = _db.Customers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(vehicleNumber))
        {
            var vn = vehicleNumber.Trim().ToLower();
            q = q.Where(c => c.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(vn)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.Phone.Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.NationalId != null && c.NationalId.ToLower().Contains(s)));
        }

        var items = await q.OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerListItemDto(
                c.Id, c.FullName, c.Phone, c.Email, c.Vehicles.Count, c.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(int id)
    {
        var c = await _db.Customers.AsNoTracking()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c is null) return NotFound();
        return ToDetail(c);
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<CustomerHistoryDto>> History(int id)
    {
        var c = await _db.Customers.AsNoTracking()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c is null) return NotFound();

        var invoices = await _db.SalesInvoices.AsNoTracking()
            .Where(s => s.CustomerId == id)
            .Include(s => s.Items)
            .Include(s => s.Vehicle)
            .OrderByDescending(s => s.InvoiceDate)
            .ToListAsync();

        var summaries = invoices.Select(s => new InvoiceSummaryDto(
            s.Id,
            s.InvoiceNumber,
            s.InvoiceDate,
            s.TotalAmount,
            s.PaymentStatus.ToString(),
            s.Items.Count,
            s.Vehicle?.VehicleNumber)).ToList();

        var totalSpent = invoices.Sum(s => s.TotalAmount);

        return new CustomerHistoryDto(ToDetail(c), summaries, totalSpent, summaries.Count);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create([FromBody] CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            NationalId = dto.NationalId?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var vDto in dto.Vehicles)
        {
            var trimmed = vDto.VehicleNumber.Trim().ToUpper();
            var exists = await _db.Vehicles.AnyAsync(v => v.VehicleNumber == trimmed);
            if (exists)
            {
                return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });
            }
            customer.Vehicles.Add(new Vehicle
            {
                VehicleNumber = trimmed,
                Make = vDto.Make?.Trim(),
                Model = vDto.Model?.Trim(),
                Year = vDto.Year,
                VehicleType = vDto.VehicleType?.Trim(),
                Color = vDto.Color?.Trim(),
                CreatedAt = DateTime.UtcNow,
            });
        }

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, ToDetail(customer));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerDetailDto>> Update(int id, [FromBody] CustomerUpdateDto dto)
    {
        var c = await _db.Customers.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id);
        if (c is null) return NotFound();

        c.FullName = dto.FullName.Trim();
        c.Phone = dto.Phone.Trim();
        c.Email = dto.Email?.Trim();
        c.Address = dto.Address?.Trim();
        c.NationalId = dto.NationalId?.Trim();
        await _db.SaveChangesAsync();

        return ToDetail(c);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return NotFound();

        var hasInvoices = await _db.SalesInvoices.AnyAsync(s => s.CustomerId == id);
        if (hasInvoices)
        {
            return Conflict(new { message = "Customer has sales invoices and cannot be deleted." });
        }

        _db.Customers.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/vehicles")]
    public async Task<ActionResult<VehicleDto>> AddVehicle(int id, [FromBody] VehicleUpsertDto dto)
    {
        var c = await _db.Customers.FindAsync(id);
        if (c is null) return NotFound();

        var trimmed = dto.VehicleNumber.Trim().ToUpper();
        if (await _db.Vehicles.AnyAsync(v => v.VehicleNumber == trimmed))
        {
            return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });
        }

        var v = new Vehicle
        {
            CustomerId = id,
            VehicleNumber = trimmed,
            Make = dto.Make?.Trim(),
            Model = dto.Model?.Trim(),
            Year = dto.Year,
            VehicleType = dto.VehicleType?.Trim(),
            Color = dto.Color?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id }, new VehicleDto(
            v.Id, v.CustomerId, v.VehicleNumber, v.Make, v.Model, v.Year, v.VehicleType, v.Color));
    }

    [HttpDelete("{id:int}/vehicles/{vehicleId:int}")]
    public async Task<IActionResult> RemoveVehicle(int id, int vehicleId)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == vehicleId && x.CustomerId == id);
        if (v is null) return NotFound();
        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerDetailDto ToDetail(Customer c) => new(
        c.Id, c.FullName, c.Phone, c.Email, c.Address, c.NationalId, c.CreatedAt,
        c.Vehicles.Select(v => new VehicleDto(
            v.Id, v.CustomerId, v.VehicleNumber, v.Make, v.Model, v.Year, v.VehicleType, v.Color)).ToList());
}
