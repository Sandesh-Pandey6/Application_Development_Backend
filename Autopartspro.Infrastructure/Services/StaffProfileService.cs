using Autopartspro.Application.Dtos.Staff;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class StaffProfileService : IStaffProfileService
{
    private readonly AppDbContext _context;

    public StaffProfileService(AppDbContext context) => _context = context;

    public async Task<StaffProfileDto> GetProfileAsync(Guid staffUserId)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == staffUserId && u.Role == RoleType.Staff)
            ?? throw new KeyNotFoundException("Staff account not found.");

        return Map(user);
    }

    public async Task<StaffProfileDto> UpdateProfileAsync(Guid staffUserId, UpdateStaffProfileDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == staffUserId && u.Role == RoleType.Staff)
            ?? throw new KeyNotFoundException("Staff account not found.");

        if (dto.FullName != null)
        {
            var name = dto.FullName.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Full name is required.");
            user.FullName = name;
        }

        if (dto.PhoneNumber != null)
            user.PhoneNumber = dto.PhoneNumber.Trim();

        if (dto.City != null)
            user.City = dto.City.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Map(user);
    }

    private static StaffProfileDto Map(Domain.Entities.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        City = user.City,
        ProfileImageUrl = user.ProfileImageUrl,
    };
}
