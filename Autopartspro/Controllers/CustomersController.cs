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
[Route("api/customers")]
[Authorize(Roles = "Admin,Staff")]
public class CustomersController : ControllerBase
{
    public const string StaffRegisteredDefaultPassword = "1234567";

    private readonly AppDbContext _db;

    private readonly IUserNotificationService _notifications;

    public CustomersController(AppDbContext db, IUserNotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    private IQueryable<User> CustomersQuery() =>
        _db.Users.AsNoTracking().Where(u => u.Role == RoleType.Customer);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CustomerListItemDto>>> List(
        [FromQuery] string? search,
        [FromQuery] string? vehicleNumber)
    {
        var q = CustomersQuery().Include(c => c.Vehicles).AsQueryable();

        if (!string.IsNullOrWhiteSpace(vehicleNumber))
        {
            var vn = vehicleNumber.Trim().ToLower();
            q = q.Where(c => c.Vehicles.Any(v => v.NumberPlate.ToLower().Contains(vn)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            q = q.Where(c =>
                c.FullName.ToLower().Contains(s) ||
                c.PhoneNumber.Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.City.ToLower().Contains(s) ||
                c.Id.ToString().Contains(s));
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
        var c = await CustomersQuery()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (c is null) return NotFound();
        return ToDetail(c);
    }

    [HttpGet("{id:guid}/history")]
    public async Task<ActionResult<CustomerHistoryDto>> History(Guid id)
    {
        var c = await CustomersQuery()
            .Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id);
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
            s.Items.Count)).ToList();

        var totalSpent = invoices.Sum(s => s.TotalAmount);

        return new CustomerHistoryDto(ToDetail(c), summaries, totalSpent, summaries.Count);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDetailDto>> Create([FromBody] CustomerCreateDto dto)
    {
        var customer = new User
        {
            FullName = dto.FullName.Trim(),
            PhoneNumber = dto.Phone.Trim(),
            Email = dto.Email?.Trim().ToLowerInvariant() ?? $"{Guid.NewGuid():N}@customer.local",
            City = dto.City?.Trim() ?? string.Empty,
            Role = RoleType.Customer,
            Status = StatusType.Active,
            IsEmailVerified = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(StaffRegisteredDefaultPassword),
            MustChangePassword = true,
        };

        foreach (var vDto in dto.Vehicles)
        {
            var trimmed = vDto.NumberPlate.Trim();
            if (await _db.Vehicles.AnyAsync(v => v.NumberPlate.ToLower() == trimmed.ToLower()))
                return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });

            var fuelType = Enum.TryParse<FuelType>(vDto.FuelType, true, out var ft)
                ? ft : FuelType.Petrol;

            customer.Vehicles.Add(new Vehicle
            {
                NumberPlate = trimmed,
                Make = vDto.Make?.Trim() ?? string.Empty,
                Model = vDto.Model?.Trim() ?? string.Empty,
                Year = vDto.Year ?? 0,
                FuelType = fuelType,
            });
        }

        _db.Users.Add(customer);
        await _db.SaveChangesAsync();

        await _notifications.NotifyUserAsync(
            customer.Id,
            "Welcome to AutoParts Pro. Your account was created by our team. Sign in with your phone number and the temporary password you were given, then change your password.",
            NotificationType.General);

        await _notifications.NotifyAdminsAndStaffAsync(
            $"New customer registered: {customer.FullName} ({customer.PhoneNumber}).",
            NotificationType.General);

        return CreatedAtAction(nameof(Get), new { id = customer.Id }, ToDetail(customer));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CustomerDetailDto>> Update(Guid id, [FromBody] CustomerUpdateDto dto)
    {
        var c = await _db.Users.Include(c => c.Vehicles)
            .FirstOrDefaultAsync(c => c.Id == id && c.Role == RoleType.Customer);
        if (c is null) return NotFound();

        c.FullName = dto.FullName.Trim();
        c.PhoneNumber = dto.Phone.Trim();
        c.Email = dto.Email?.Trim().ToLowerInvariant() ?? c.Email;
        c.City = dto.City?.Trim() ?? string.Empty;
        c.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ToDetail(c);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var c = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == RoleType.Customer);
        if (c is null) return NotFound();

        if (await _db.SalesInvoices.AnyAsync(s => s.CustomerId == id))
            return Conflict(new { message = "Customer has sales invoices and cannot be deleted." });

        _db.Users.Remove(c);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/vehicles")]
    public async Task<ActionResult<VehicleDto>> AddVehicle(Guid id, [FromBody] VehicleUpsertDto dto)
    {
        var c = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Role == RoleType.Customer);
        if (c is null) return NotFound();

        var trimmed = dto.NumberPlate.Trim();
        if (await _db.Vehicles.AnyAsync(v => v.NumberPlate.ToLower() == trimmed.ToLower()))
            return Conflict(new { message = $"Vehicle number '{trimmed}' is already registered." });

        var fuelType = Enum.TryParse<FuelType>(dto.FuelType, true, out var ft)
            ? ft : FuelType.Petrol;

        var v = new Vehicle
        {
            CustomerId = id,
            NumberPlate = trimmed,
            Make = dto.Make?.Trim() ?? string.Empty,
            Model = dto.Model?.Trim() ?? string.Empty,
            Year = dto.Year ?? 0,
            FuelType = fuelType,
        };
        _db.Vehicles.Add(v);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id },
            new VehicleDto(v.Id, v.CustomerId, v.NumberPlate, v.Make, v.Model, v.Year, v.FuelType.ToString()));
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
        c.Id,
        c.FullName,
        c.PhoneNumber,
        c.Email,
        c.City,
        c.CreatedAt,
        c.Vehicles.Select(v => new VehicleDto(
            v.Id, v.CustomerId, v.NumberPlate, v.Make, v.Model, v.Year, v.FuelType.ToString())).ToList());
}
