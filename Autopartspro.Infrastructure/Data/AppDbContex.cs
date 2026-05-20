using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<StaffEmployment> StaffEmployments => Set<StaffEmployment>();
        public DbSet<Vendor> Vendors => Set<Vendor>();
        public DbSet<Part> Parts => Set<Part>();
        public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
        public DbSet<PurchaseInvoiceItem> PurchaseInvoiceItems => Set<PurchaseInvoiceItem>();
        public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
        public DbSet<SalesInvoiceItem> SalesInvoiceItems => Set<SalesInvoiceItem>();
        public DbSet<Appointment> Appointments => Set<Appointment>();
        public DbSet<PartRequest> PartRequests => Set<PartRequest>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();
        public PurchaseInvoiceStatus Status { get; set; } = PurchaseInvoiceStatus.Pending;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //  Unique constraints 
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => v.NumberPlate).IsUnique();

            modelBuilder.Entity<StaffEmployment>()
                .HasIndex(s => s.EmployeeId).IsUnique();

            modelBuilder.Entity<SalesInvoice>()
                .HasIndex(s => s.InvoiceNumber).IsUnique();

            modelBuilder.Entity<PurchaseInvoice>()
                .HasIndex(p => p.InvoiceNumber).IsUnique();

            //  Decimal precision 
            modelBuilder.Entity<Part>()
                .Property(p => p.Price).HasPrecision(18, 2);

            modelBuilder.Entity<SalesInvoice>()
                .Property(s => s.SubTotal).HasPrecision(18, 2);
            modelBuilder.Entity<SalesInvoice>()
                .Property(s => s.DiscountAmount).HasPrecision(18, 2);
            modelBuilder.Entity<SalesInvoice>()
                .Property(s => s.TotalAmount).HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseInvoice>()
                .Property(p => p.TotalAmount).HasPrecision(18, 2);

            modelBuilder.Entity<SalesInvoiceItem>()
                .Property(s => s.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<SalesInvoiceItem>()
                .Property(s => s.SubTotal).HasPrecision(18, 2);

            modelBuilder.Entity<PurchaseInvoiceItem>()
                .Property(p => p.UnitPrice).HasPrecision(18, 2);
            modelBuilder.Entity<PurchaseInvoiceItem>()
                .Property(p => p.SubTotal).HasPrecision(18, 2);

            //  StaffEmployment → User (one-to-one) 
            modelBuilder.Entity<StaffEmployment>()
                .HasOne(s => s.User)
                .WithOne(u => u.StaffEmployment)
                .HasForeignKey<StaffEmployment>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            //  SalesInvoice → Customer 
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(s => s.Customer)
                .WithMany(u => u.CustomerInvoices)
                .HasForeignKey(s => s.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            //  SalesInvoice → Staff 
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(s => s.Staff)
                .WithMany(u => u.StaffInvoices)
                .HasForeignKey(s => s.StaffId)
                .OnDelete(DeleteBehavior.Restrict);

            //  SalesInvoice → Vehicle (optional)
            modelBuilder.Entity<SalesInvoice>()
                .HasOne(s => s.Vehicle)
                .WithMany(v => v.SalesInvoices)
                .HasForeignKey(s => s.VehicleId)
                .OnDelete(DeleteBehavior.SetNull);

            //  PurchaseInvoice → Admin
            modelBuilder.Entity<PurchaseInvoice>()
                .HasOne(p => p.Admin)
                .WithMany(u => u.PurchaseInvoices)
                .HasForeignKey(p => p.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            //  Review optional FKs 
            modelBuilder.Entity<Review>()
                .HasOne(r => r.RelatedInvoice)
                .WithMany(s => s.Reviews)
                .HasForeignKey(r => r.RelatedInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.RelatedAppointment)
                .WithMany(a => a.Reviews)
                .HasForeignKey(r => r.RelatedAppointmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // OTP → User (by email)
            modelBuilder.Entity<OtpVerification>()
                .HasIndex(o => new { o.Email, o.Purpose });

            modelBuilder.Entity<OtpVerification>()
                .HasOne<User>()
                .WithMany(u => u.OtpVerifications)
                .HasForeignKey(o => o.Email)
                .HasPrincipalKey(u => u.Email)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PartRequest>()
                .HasOne(p => p.Vendor)
                .WithMany()
                .HasForeignKey(p => p.VendorId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PartRequest>()
                .HasOne(p => p.PurchaseInvoice)
                .WithMany()
                .HasForeignKey(p => p.PurchaseInvoiceId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}