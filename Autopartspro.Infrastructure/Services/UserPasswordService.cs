using Autopartspro.Application.Dtos.Auth;
using Autopartspro.Application.Interfaces;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Services;

public class UserPasswordService : IUserPasswordService
{
    private readonly AppDbContext _context;
    private readonly ICustomerService _customerService;

    public UserPasswordService(AppDbContext context, ICustomerService customerService)
    {
        _context = context;
        _customerService = customerService;
    }

    public async Task<UserProfileDto> ChangePasswordAsync(
        Guid userId,
        ChangePasswordDto dto,
        params RoleType[] allowedRoles)
    {
        if (allowedRoles.Length == 0)
            allowedRoles = [RoleType.Customer, RoleType.Staff, RoleType.Admin];

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            throw new KeyNotFoundException("User not found.");

        if (!allowedRoles.Contains(user.Role))
            throw new ArgumentException("This account cannot use this password change endpoint.");

        if (user.Role == RoleType.Customer)
            return await _customerService.ChangePasswordAsync(userId, dto);

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            throw new ArgumentException("New password must be at least 6 characters.");

        if (dto.NewPassword != dto.ConfirmNewPassword)
            throw new ArgumentException("New password and confirmation do not match.");

        var tracked = await _context.Users.FirstAsync(u => u.Id == userId);

        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, tracked.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");

        if (BCrypt.Net.BCrypt.Verify(dto.NewPassword, tracked.PasswordHash))
            throw new ArgumentException("Choose a password different from your current password.");

        tracked.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        tracked.MustChangePassword = false;
        tracked.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new UserProfileDto
        {
            Id = tracked.Id,
            FullName = tracked.FullName,
            Email = tracked.Email,
            PhoneNumber = tracked.PhoneNumber,
            City = tracked.City,
            Role = tracked.Role.ToString(),
            MustChangePassword = tracked.MustChangePassword,
        };
    }
}
