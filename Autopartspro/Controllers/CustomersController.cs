using Autopartspro.Application.Dtos;
using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
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
        [FromQuery] string? numberPlate)
    {
        var q = _db.Users.AsNoTracking().Where(u => u.Role == RoleType.Customer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(numberPlate))
        {
            var vn = numberPlate.Trim().ToLower();
            q = q.Where(c => c.Vehicles.Any(v => v.NumberPlate.ToLower().Contains(vn)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.PhoneNumber.Contains(s) ||
                (c.Email != null && c.Email.ToLower().Contains(s)) ||
                (c.City != null && c.City.ToLower().Contains(s)));
        }

        var items = await q.OrderByDescending(c => c.CreatedAt)
            .Select(c => new CustomerListItemDto(
                c.Id, c.FullName, c.PhoneNumber, c.Email, c.Vehicles.Count, c.CreatedAt))
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Get(Guid id)
    {
        var c = await _db.Users.AsNoTracking()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id && c.Role == RoleType.Customer);
        if (c is null) return NotFound();
        return ToDetail(c);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<CustomerHistoryDto>> History(Guid id)
    {
        var c = await _db.Users.AsNoTracking()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id && c.Role == RoleType.Customer);
        if (c is null) return NotFound();

        var invoices = await _db.SalesInvoices.AsNoTracking()
            .Where(s => s.CustomerId == id)
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync();

        var summaries = invoices.Select(s => new InvoiceSummaryDto(
            s.Id,
            s.InvoiceNumber,
            s.SaleDate,
            s.TotalAmount,
            s.PaymentStatus.ToString(),
            s.Items.Count,
            null)).ToList();

        var totalSpent = invoices.Sum(s => s.TotalAmount);

        return new CustomerHistoryDto(ToDetail(c), summaries, totalSpent, summaries.Count);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create([FromBody] CustomerCreateDto dto)
    {
        var customer = new User
        {
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.PhoneNumber.Trim(),
            Email = dto.Email?.Trim() ?? "",
            City = dto.City?.Trim() ?? "",
            Role = RoleType.Customer,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var vDto in dto.Vehicles)
        {
            var trimmed = vDto.NumberPlate.Trim().ToUpper();
            var exists = await _db.Vehicles.AnyAsync(v => v.NumberPlate == trimmed);
            if (exists)
            {
                return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });
            }
            FuelType parsedFuelType = FuelType.Petrol;
            if (vDto.FuelType != null) {
                Enum.TryParse<FuelType>(vDto.FuelType, true, out parsedFuelType);
            }
            customer.Vehicles.Add(new Vehicle
            {
                NumberPlate = trimmed,
                Make = vDto.Make?.Trim() ?? "",
                Model = vDto.Model?.Trim() ?? "",
                Year = vDto.Year ?? DateTime.UtcNow.Year,
                FuelType = parsedFuelType,
                CreatedAt = DateTime.UtcNow,
            });
        }

        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, ToDetail(customer));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Update(Guid id, [FromBody] CustomerUpdateDto dto)
    {
        var c = await _db.Users.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id && c.Role == RoleType.Customer);
        if (c is null) return NotFound();

        c.FullName = dto.FullName.Trim();
        c.PhoneNumber = dto.PhoneNumber.Trim();
        if (dto.Email != null) c.Email = dto.Email.Trim();
        if (dto.City != null) c.City = dto.City.Trim();
        await _db.SaveChangesAsync();

        return ToDetail(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Users.FindAsync(id);
        if (c is null || c.Role != RoleType.Customer) return NotFound();

        var hasInvoices = await _db.SalesInvoices.AnyAsync(s => s.CustomerId == id);
        if (hasInvoices)
        {
            return Conflict(new { message = "Customer has sales invoices and cannot be deleted." });
        }

        _db.Users.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/vehicles")]
    public async Task<ActionResult<VehicleDto>> AddVehicle(Guid id, [FromBody] VehicleUpsertDto dto)
    {
        var c = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == RoleType.Customer);
        if (c is null) return NotFound();

        var trimmed = dto.NumberPlate.Trim().ToUpper();
        if (await _db.Vehicles.AnyAsync(v => v.NumberPlate == trimmed))
        {
            return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });
        }

        FuelType parsedFuelType = FuelType.Petrol;
        if (dto.FuelType != null) {
            Enum.TryParse<FuelType>(dto.FuelType, true, out parsedFuelType);
        }

        var v = new Vehicle
        {
            CustomerId = id,
            NumberPlate = trimmed,
            Make = dto.Make?.Trim() ?? "",
            Model = dto.Model?.Trim() ?? "",
            Year = dto.Year ?? DateTime.UtcNow.Year,
            FuelType = parsedFuelType,
            CreatedAt = DateTime.UtcNow,
        };
        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id }, new VehicleDto(
            v.Id, v.CustomerId, v.NumberPlate, v.Make, v.Model, v.Year, v.FuelType.ToString()));
    }

    [HttpDelete("{id:guid}/vehicles/{vehicleId:guid}")]
    public async Task<IActionResult> RemoveVehicle(Guid id, Guid vehicleId)
    {
        var v = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == vehicleId && x.CustomerId == id);
        if (v is null) return NotFound();
        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static CustomerDetailDto ToDetail(User c) => new(
        c.Id, c.FullName, c.PhoneNumber, c.Email, c.City, c.CreatedAt,
        c.Vehicles.Select(v => new VehicleDto(
            v.Id, v.CustomerId, v.NumberPlate, v.Make, v.Model, v.Year, v.FuelType.ToString())).ToList());
}
