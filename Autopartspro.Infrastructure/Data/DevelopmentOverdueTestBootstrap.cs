using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Autopartspro.Infrastructure.Data;

/// <summary>
/// Development-only: ensures testUser2 has an unpaid sales invoice dated 3+ days ago for overdue reminder testing.
/// </summary>
public static class DevelopmentOverdueTestBootstrap
{
    public const string TestInvoiceNumber = "SI-DEV-OVERDUE-TEST";

    public static async Task EnsureAsync(AppDbContext db, IConfiguration config, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        if (!config.GetValue("BootstrapOverdueTest:Enabled", true))
            return;

        var customerEmail = (config["BootstrapOverdueTest:CustomerEmail"] ?? "").Trim();
        var customerName = config["BootstrapOverdueTest:CustomerName"] ?? "testUser2";

        var customer = await db.Users
            .Where(u => u.Role == RoleType.Customer)
            .Where(u =>
                EF.Functions.ILike(u.FullName, "%testuser2%") ||
                EF.Functions.ILike(u.Email, "%testuser2%") ||
                (!string.IsNullOrEmpty(customerEmail) && u.Email.ToLower() == customerEmail.ToLower()))
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync();

        if (customer is null)
        {
            var email = string.IsNullOrWhiteSpace(customerEmail)
                ? "testuser2@example.com"
                : customerEmail.ToLowerInvariant();

            customer = new User
            {
                FullName = customerName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@123"),
                PhoneNumber = "9800000002",
                City = "Kathmandu",
                Role = RoleType.Customer,
                Status = StatusType.Active,
                IsEmailVerified = true,
            };
            db.Users.Add(customer);
            await db.SaveChangesAsync();
            Console.WriteLine($"BootstrapOverdueTest: created customer {customer.FullName} ({customer.Email}).");
        }

        if (string.IsNullOrWhiteSpace(customer.Email) ||
            customer.Email.EndsWith("@customer.local", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(
                $"BootstrapOverdueTest: customer {customer.FullName} has no deliverable email ({customer.Email}). " +
                "Set BootstrapOverdueTest:CustomerEmail in appsettings.Development.json for overdue email tests.");
        }

        var staff = await db.Users
            .Where(u => u.Role == RoleType.Staff || u.Role == RoleType.Admin)
            .Where(u => u.Status == StatusType.Active)
            .OrderBy(u => u.Role == RoleType.Staff ? 0 : 1)
            .FirstOrDefaultAsync();

        if (staff is null)
        {
            Console.WriteLine("BootstrapOverdueTest: no staff/admin user found; skip overdue test invoice.");
            return;
        }

        var part = await db.Parts
            .Where(p => p.StockQuantity > 0)
            .OrderBy(p => p.PartName)
            .FirstOrDefaultAsync();

        if (part is null)
        {
            Console.WriteLine("BootstrapOverdueTest: no parts in stock; run sample data seed first.");
            return;
        }

        var overdueDays = Math.Max(1, config.GetValue("PaymentReminders:OverdueDays", 3));
        var saleDate = DateTime.UtcNow.Date.AddDays(-overdueDays);

        var existing = await db.SalesInvoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == TestInvoiceNumber);

        if (existing is not null)
        {
            existing.CustomerId = customer.Id;
            existing.StaffId = staff.Id;
            existing.PaymentStatus = PaymentStatus.Unpaid;
            existing.SaleDate = saleDate;
            existing.OverdueReminderSentAt = null;
            existing.UpdatedAt = DateTime.UtcNow;

            if (existing.Items.Count == 0)
            {
                existing.Items.Add(new SalesInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = 1,
                    UnitPrice = part.Price,
                    SubTotal = part.Price,
                });
            }

            existing.SubTotal = existing.Items.Sum(i => i.SubTotal);
            existing.TotalAmount = existing.SubTotal - existing.DiscountAmount;
            if (existing.TotalAmount < 0) existing.TotalAmount = existing.SubTotal;

            await db.SaveChangesAsync();
            Console.WriteLine(
                $"BootstrapOverdueTest: refreshed invoice {TestInvoiceNumber} for {customer.FullName}, sale {saleDate:yyyy-MM-dd} UTC, unpaid.");
            return;
        }

        var lineTotal = part.Price;
        var invoice = new SalesInvoice
        {
            InvoiceNumber = TestInvoiceNumber,
            CustomerId = customer.Id,
            StaffId = staff.Id,
            SaleDate = saleDate,
            PaymentStatus = PaymentStatus.Unpaid,
            SubTotal = lineTotal,
            DiscountApplied = false,
            DiscountAmount = 0,
            TotalAmount = lineTotal,
            OverdueReminderSentAt = null,
            Items =
            {
                new SalesInvoiceItem
                {
                    PartId = part.Id,
                    Quantity = 1,
                    UnitPrice = part.Price,
                    SubTotal = lineTotal,
                },
            },
        };

        db.SalesInvoices.Add(invoice);
        await db.SaveChangesAsync();

        Console.WriteLine(
            $"BootstrapOverdueTest: created unpaid invoice {TestInvoiceNumber} — {part.PartName}, " +
            $"Rs. {lineTotal:N2}, sale {saleDate:yyyy-MM-dd} UTC, customer {customer.Email}.");
    }
}
