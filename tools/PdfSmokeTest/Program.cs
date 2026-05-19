using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using Autopartspro.Infrastructure.Services;

var invoice = new SalesInvoice
{
    InvoiceNumber = "INV-SMOKE-001",
    SaleDate = DateTime.UtcNow,
    PaymentStatus = PaymentStatus.Paid,
    SubTotal = 3200m,
    DiscountAmount = 320m,
    TotalAmount = 2880m,
    Customer = new User { FullName = "Test Customer", PhoneNumber = "9800000000", Email = "test@example.com" },
    Staff = new User { FullName = "Staff User" },
    Items =
    [
        new SalesInvoiceItem
        {
            Quantity = 1,
            UnitPrice = 3200m,
            SubTotal = 3200m,
            Part = new Part { PartName = "Brembo Brake Pad Set" },
        },
    ],
};

var pdf = InvoicePdfGenerator.Build(invoice);
var header = System.Text.Encoding.ASCII.GetString(pdf, 0, 8);
Console.WriteLine($"Bytes: {pdf.Length}");
Console.WriteLine($"Header: {header}");
Console.WriteLine($"Valid: {InvoicePdfGenerator.IsValidPdf(pdf)}");
var outPath = Path.Combine(Path.GetTempPath(), "autopartspro-smoke.pdf");
await File.WriteAllBytesAsync(outPath, pdf);
Console.WriteLine($"Wrote: {outPath}");
