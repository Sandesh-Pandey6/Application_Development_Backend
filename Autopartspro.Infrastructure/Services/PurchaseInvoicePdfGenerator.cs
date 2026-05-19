using Autopartspro.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Autopartspro.Infrastructure.Services;

public static class PurchaseInvoicePdfGenerator
{
    public static byte[] Build(PurchaseInvoice invoice)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("AutoParts Pro").FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Vendor Purchase Invoice").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Invoice #: {invoice.InvoiceNumber}").Bold().FontSize(11);
                            left.Item().Text($"Purchase date: {invoice.PurchaseDate:dd MMM yyyy, HH:mm}");
                            left.Item().Text($"Status: {invoice.Status}").SemiBold();
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Vendor").SemiBold();
                            right.Item().Text(invoice.Vendor.VendorName);
                            if (!string.IsNullOrWhiteSpace(invoice.Vendor.PhoneNumber))
                                right.Item().Text($"Phone: {invoice.Vendor.PhoneNumber}");
                            if (!string.IsNullOrWhiteSpace(invoice.Vendor.Email))
                                right.Item().Text($"Email: {invoice.Vendor.Email}");
                            if (!string.IsNullOrWhiteSpace(invoice.Vendor.Address))
                                right.Item().Text(invoice.Vendor.Address);
                        });
                    });

                    col.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.ConstantColumn(45);
                            columns.ConstantColumn(75);
                            columns.ConstantColumn(75);
                        });

                        table.Header(header =>
                        {
                            static IContainer HeaderCell(IContainer c) =>
                                c.DefaultTextStyle(x => x.SemiBold()).Background(Colors.Grey.Lighten3).Padding(6);

                            header.Cell().Element(HeaderCell).Text("Part");
                            header.Cell().Element(HeaderCell).AlignCenter().Text("Qty");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Unit (Rs.)");
                            header.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                        });

                        foreach (var line in invoice.Items)
                        {
                            static IContainer BodyCell(IContainer c) =>
                                c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(6);

                            table.Cell().Element(BodyCell).Text(line.Part?.PartName ?? "Part");
                            table.Cell().Element(BodyCell).AlignCenter().Text(line.Quantity.ToString());
                            table.Cell().Element(BodyCell).AlignRight().Text($"Rs. {line.UnitPrice:N2}");
                            table.Cell().Element(BodyCell).AlignRight().Text($"Rs. {line.SubTotal:N2}");
                        }
                    });

                    col.Item().PaddingTop(16).AlignRight().Width(220).Column(totals =>
                    {
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total:").Bold().FontSize(12);
                            r.ConstantItem(90).AlignRight().Text($"Rs. {invoice.TotalAmount:N2}").Bold().FontSize(12);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Internal purchase record - ");
                    text.Span("AutoParts Pro").SemiBold();
                });
            });
        });

        var pdf = document.GeneratePdf();
        if (!InvoicePdfGenerator.IsValidPdf(pdf))
            throw new InvalidOperationException("Failed to generate purchase invoice PDF.");

        return pdf;
    }
}
