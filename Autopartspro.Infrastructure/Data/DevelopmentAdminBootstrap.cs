using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Autopartspro.Infrastructure.Data;

/// <summary>
/// Ensures a default admin account exists in Development (no public admin registration).
/// </summary>
public static class DevelopmentAdminBootstrap
{
    public static async Task EnsureAsync(
        AppDbContext db,
        IConfiguration config,
        IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        var email = (config["BootstrapAdmin:Email"] ?? "admin@autopartspro.com").Trim().ToLowerInvariant();
        var password = config["BootstrapAdmin:Password"] ?? "Admin@123";
        var fullName = config["BootstrapAdmin:FullName"] ?? "System Administrator";

        var existing = await db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (existing != null)
        {
            if (existing.Role != RoleType.Admin)
            {
                Console.WriteLine(
                    $"BootstrapAdmin: {email} exists as {existing.Role}; use another email or remove that user.");
                return;
            }

            existing.IsEmailVerified = true;
            existing.Status = StatusType.Active;
            if (!BCrypt.Net.BCrypt.Verify(password, existing.PasswordHash))
                existing.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            await db.SaveChangesAsync();
            Console.WriteLine($"Development admin verified: {email}");
            return;
        }

        db.Users.Add(new User
        {
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PhoneNumber = string.Empty,
            City = "Kathmandu",
            Role = RoleType.Admin,
            Status = StatusType.Active,
            IsEmailVerified = true
        });

        await db.SaveChangesAsync();
        Console.WriteLine($"Development admin created: {email} (password from BootstrapAdmin:Password)");
    }
}
