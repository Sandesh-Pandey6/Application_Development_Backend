using Autopartspro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext db)
    {
        if (await db.Vendors.AnyAsync()) return;

        var v1 = new Vendor { VendorName = "AutoParts Nepal", ContactPerson = "Ramesh K.", Email = "sales@autopartsnp.com", PhoneNumber = "9841000001", Address = "Balaju, Kathmandu" };
        var v2 = new Vendor { VendorName = "Himalayan Spares", ContactPerson = "Sita L.", Email = "info@himspares.com", PhoneNumber = "9841000002", Address = "Lalitpur" };
        db.Vendors.AddRange(v1, v2);
        await db.SaveChangesAsync();

        db.Parts.AddRange(
            new Part { PartName = "Brake Pad Set", Price = 1200m, StockQuantity = 25, VendorId = v1.Id, Description = "Front brake pads" },
            new Part { PartName = "Engine Oil 1L", Price = 850m,  StockQuantity = 60, VendorId = v1.Id, Description = "10W-30 mineral oil" },
            new Part { PartName = "Air Filter",    Price = 650m,  StockQuantity = 40, VendorId = v2.Id },
            new Part { PartName = "Spark Plug (4)", Price = 1500m, StockQuantity = 15, VendorId = v2.Id }
        );
        await db.SaveChangesAsync();
    }
}
