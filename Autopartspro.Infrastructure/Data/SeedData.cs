using Autopartspro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Autopartspro.Infrastructure.Data;

public static class SeedData
{
    public static async Task EnsureSeedAsync(AppDbContext db)
    {
        if (await db.Vendors.AnyAsync()) return;

        var v1 = new Vendor { Name = "AutoParts Nepal", ContactPerson = "Ramesh K.", Email = "sales@autopartsnp.com", Phone = "9841000001", Address = "Balaju, Kathmandu" };
        var v2 = new Vendor { Name = "Himalayan Spares", ContactPerson = "Sita L.", Email = "info@himspares.com", Phone = "9841000002", Address = "Lalitpur" };
        db.Vendors.AddRange(v1, v2);
        await db.SaveChangesAsync();

        db.Parts.AddRange(
            new Part { Name = "Brake Pad Set",  PartCode = "BP-001", Price = 1200m, StockQuantity = 25, VendorId = v1.Id, Description = "Front brake pads" },
            new Part { Name = "Engine Oil 1L",  PartCode = "OIL-1L", Price = 850m,  StockQuantity = 60, VendorId = v1.Id, Description = "10W-30 mineral oil" },
            new Part { Name = "Air Filter",     PartCode = "AF-110", Price = 650m,  StockQuantity = 40, VendorId = v2.Id },
            new Part { Name = "Spark Plug (4)", PartCode = "SP-4",   Price = 1500m, StockQuantity = 15, VendorId = v2.Id }
        );
        await db.SaveChangesAsync();
    }
}
