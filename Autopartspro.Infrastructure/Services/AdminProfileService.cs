using System.Net.Mail;
using Autopartspro.Application.Dtos.Admin;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class AdminProfileService : IAdminProfileService
{
    private readonly AppDbContext _context;

    public AdminProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AdminProfileDto> GetProfileAsync(Guid adminUserId)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == RoleType.Admin);

        if (user == null)
            throw new KeyNotFoundException("Admin account not found.");

        return Map(user);
    }

    public async Task<AdminProfileDto> UpdateProfileAsync(Guid adminUserId, UpdateAdminProfileDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == adminUserId && u.Role == RoleType.Admin);

        if (user == null)
            throw new KeyNotFoundException("Admin account not found.");

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

        if (dto.BusinessEmail != null)
        {
            var businessEmail = dto.BusinessEmail.Trim();
            if (string.IsNullOrWhiteSpace(businessEmail))
            {
                user.BusinessEmail = null;
            }
            else
            {
                if (!IsValidEmail(businessEmail))
                    throw new ArgumentException("Enter a valid business email address.");
                user.BusinessEmail = businessEmail.ToLowerInvariant();
            }
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Map(user);
    }

    private static AdminProfileDto Map(Domain.Entities.User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        BusinessEmail = user.BusinessEmail,
        PhoneNumber = user.PhoneNumber,
        City = user.City,
    };

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
