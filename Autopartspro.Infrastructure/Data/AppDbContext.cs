using Autopartspro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Vendor>()
            .HasIndex(v => v.Name);

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Phone);

        modelBuilder.Entity<Vehicle>()
            .HasIndex(v => v.VehicleNumber)
            .IsUnique();

        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Customer)
            .WithMany(c => c.Vehicles)
            .HasForeignKey(v => v.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Part>()
            .HasOne(p => p.Vendor)
            .WithMany(v => v.Parts)
            .HasForeignKey(p => p.VendorId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SalesInvoice>()
            .HasIndex(s => s.InvoiceNumber)
            .IsUnique();

        modelBuilder.Entity<SalesInvoice>()
            .HasOne(s => s.Customer)
            .WithMany(c => c.SalesInvoices)
            .HasForeignKey(s => s.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SalesInvoice>()
            .HasOne(s => s.Vehicle)
            .WithMany()
            .HasForeignKey(s => s.VehicleId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<SalesInvoiceItem>()
            .HasOne(i => i.SalesInvoice)
            .WithMany(s => s.Items)
            .HasForeignKey(i => i.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SalesInvoiceItem>()
            .HasOne(i => i.Part)
            .WithMany()
            .HasForeignKey(i => i.PartId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
