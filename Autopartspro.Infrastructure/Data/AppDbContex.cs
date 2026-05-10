using Autopartspro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<CustomerProfile> CustomerProfiles => Set<CustomerProfile>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PartRequest> PartRequests => Set<PartRequest>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Purchase> Purchases => Set<Purchase>();

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

        // CustomerProfile 
        modelBuilder.Entity<CustomerProfile>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasOne(c => c.User)
             .WithOne(u => u.CustomerProfile)
             .HasForeignKey<CustomerProfile>(c => c.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // StaffProfile
        modelBuilder.Entity<StaffProfile>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.User)
             .WithOne(u => u.StaffProfile)
             .HasForeignKey<StaffProfile>(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(s => s.EmployeeId).IsUnique();
        });

        // Vehicle 
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

        // Appointment
        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasOne(a => a.CustomerProfile)
             .WithMany(c => c.Appointments)
             .HasForeignKey(a => a.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(a => a.CustomerId);
            e.HasIndex(a => a.Status);
            e.Property(a => a.ServiceType).HasMaxLength(100).IsRequired();
            e.Property(a => a.Status).HasMaxLength(20).IsRequired();
        });

        // PartRequest
        modelBuilder.Entity<PartRequest>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.CustomerProfile)
             .WithMany(c => c.PartRequests)
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.CustomerId);
            e.HasIndex(p => p.Status);
            e.Property(p => p.RequestedPartName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
        });

        // Review
        modelBuilder.Entity<Review>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasOne(r => r.CustomerProfile)
             .WithMany(c => c.Reviews)
             .HasForeignKey(r => r.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Appointment)
             .WithMany(a => a.Reviews)
             .HasForeignKey(r => r.AppointmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(r => r.CustomerId);
            e.HasIndex(r => r.AppointmentId);
        });

        // Notification
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(n => n.IsRead);
        });

        // Invoice
        modelBuilder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasOne(i => i.CustomerProfile)
             .WithMany(c => c.Invoices)
             .HasForeignKey(i => i.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(i => i.CustomerId);
            e.HasIndex(i => i.PaymentStatus);
            e.HasIndex(i => i.InvoiceDate);
            e.Property(i => i.InvoiceNumber).HasMaxLength(50).IsRequired();
            e.Property(i => i.PaymentStatus).HasMaxLength(20).IsRequired();
            e.Property(i => i.OriginalAmount).HasPrecision(18, 2);
            e.Property(i => i.DiscountAmount).HasPrecision(18, 2);
            e.Property(i => i.FinalAmount).HasPrecision(18, 2);
        });

        // Purchase
        modelBuilder.Entity<Purchase>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.CustomerProfile)
             .WithMany(c => c.Purchases)
             .HasForeignKey(p => p.CustomerId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.CustomerId);
            e.HasIndex(p => p.PurchaseDate);
            e.Property(p => p.PartName).HasMaxLength(200).IsRequired();
            e.Property(p => p.Status).HasMaxLength(20).IsRequired();
            e.Property(p => p.UnitPrice).HasPrecision(18, 2);
            e.Property(p => p.TotalPrice).HasPrecision(18, 2);
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
