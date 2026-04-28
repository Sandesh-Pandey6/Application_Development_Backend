using Autopartspro.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace Autopartspro.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasMaxLength(20).IsRequired();
            e.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(150).IsRequired();
        });

        //  CustomerProfile 
        modelBuilder.Entity<CustomerProfile>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.User)
             .WithOne(u => u.CustomerProfile)
             .HasForeignKey<CustomerProfile>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        //  StaffProfile
        modelBuilder.Entity<StaffProfile>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.User)
             .WithOne(u => u.StaffProfile)
             .HasForeignKey<StaffProfile>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.EmployeeId).IsUnique();
        });

        //  Vehicle 
        modelBuilder.Entity<Vehicle>(e =>
        {
            e.HasKey(v => v.Id);
            e.HasOne(v => v.CustomerProfile)
             .WithMany(c => c.Vehicles)
             .HasForeignKey(v => v.CustomerProfileId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(v => v.NumberPlate);
        });

        // OtpCode 
        modelBuilder.Entity<OtpCode>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasOne(o => o.User)
             .WithOne(u => u.OtpCode)
             .HasForeignKey<OtpCode>(o => o.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Seed Admin 
        var adminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminId,
            FullName = "Super Admin",
            Email = "admin@autopartspro.com",
            Phone = "9800000000",
            City = "Kathmandu",
            // BCrypt hash of "Admin@123" — change this after first login
            PasswordHash = "$2a$11$Kd3Z6qL.Jx9u1Hv2Oy5GOeIpL7mXkN4wRtY8sVbC0dFjA3nEqMzu",
            Role = "Admin",
            IsEmailVerified = true,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}