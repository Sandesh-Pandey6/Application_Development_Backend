using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Autopartspro.Infrastructure.Data;

/// <summary>
/// Seeds minimal vendors and parts in Development when the catalog is empty.
/// </summary>
public static class DevelopmentSampleDataBootstrap
{
    public static async Task EnsureAsync(AppDbContext db, IHostEnvironment env)
    {
        if (!env.IsDevelopment())
            return;

        if (await db.Vendors.AnyAsync())
            return;

        var vendor = new Vendor
        {
            VendorName = "Kathmandu Auto Supply",
            ContactPerson = "Ramesh Khadka",
            Email = "orders@kathmanduauto.example",
            PhoneNumber = "9812345678",
            Address = "Teku, Kathmandu",
        };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        db.Parts.AddRange(
            new Part
            {
                PartName = "NGK Spark Plug BKR6E",
                Category = "Engine",
                Description = "Standard spark plug",
                Price = 450,
                StockQuantity = 25,
                VendorId = vendor.Id,
            },
            new Part
            {
                PartName = "Brembo Brake Pad Set",
                Category = "Brakes",
                Description = "Front brake pads",
                Price = 3200,
                StockQuantity = 8,
                VendorId = vendor.Id,
            },
            new Part
            {
                PartName = "Denso Oil Filter",
                Category = "Filters",
                Description = "Engine oil filter",
                Price = 650,
                StockQuantity = 5,
                VendorId = vendor.Id,
            });

        await db.SaveChangesAsync();
        Console.WriteLine("Development sample vendors and parts seeded.");
    }
}
