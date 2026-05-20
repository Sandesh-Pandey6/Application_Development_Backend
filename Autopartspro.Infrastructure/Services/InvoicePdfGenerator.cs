using Autopartspro.Domain.Entities;
using Autopartspro.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Autopartspro.Infrastructure.Services;

public static class InvoicePdfGenerator
{
    static InvoicePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static bool IsValidPdf(byte[]? data) =>
        data is { Length: >= 5 }
        && data[0] == (byte)'%'
        && data[1] == (byte)'P'
        && data[2] == (byte)'D'
        && data[3] == (byte)'F';

    public static byte[] Build(SalesInvoice invoice)
    {
        var statusLabel = invoice.PaymentStatus switch
        {
            PaymentStatus.Paid => "PAID",
            PaymentStatus.Unpaid => "UNPAID",
            _ => "UNPAID",
        };

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
                    col.Item().Text("Sales Invoice").FontSize(12).FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text($"Invoice #: {invoice.InvoiceNumber}").Bold().FontSize(11);
                            left.Item().Text($"Date: {invoice.SaleDate:dd MMM yyyy, HH:mm}");
                            left.Item().PaddingTop(4).Text($"Payment status: {statusLabel}")
                                .Bold()
                                .FontColor(invoice.PaymentStatus == PaymentStatus.Paid
                                    ? Colors.Green.Darken2
                                    : Colors.Orange.Darken2);
                        });

                        row.RelativeItem().Column(right =>
                        {
                            right.Item().Text("Bill to").SemiBold();
                            right.Item().Text(invoice.Customer.FullName);
                            if (!string.IsNullOrWhiteSpace(invoice.Customer.PhoneNumber))
                                right.Item().Text($"Phone: {invoice.Customer.PhoneNumber}");
                            if (!string.IsNullOrWhiteSpace(invoice.Customer.Email) &&
                                !invoice.Customer.Email.EndsWith("@customer.local", StringComparison.OrdinalIgnoreCase))
                                right.Item().Text($"Email: {invoice.Customer.Email}");
                            if (invoice.Staff != null && !string.IsNullOrWhiteSpace(invoice.Staff.FullName))
                                right.Item().PaddingTop(6).Text($"Served by: {invoice.Staff.FullName}");
                            var vehicleLine = VehicleDisplayHelper.FormatFull(invoice.Vehicle);
                            if (!string.IsNullOrWhiteSpace(vehicleLine))
                                right.Item().PaddingTop(6).Text($"Vehicle: {vehicleLine}");
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
                            r.RelativeItem().Text("Subtotal:");
                            r.ConstantItem(90).AlignRight().Text($"Rs. {invoice.SubTotal:N2}");
                        });
                        totals.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Discount:");
                            r.ConstantItem(90).AlignRight().Text($"- Rs. {invoice.DiscountAmount:N2}");
                        });
                        totals.Item().PaddingTop(4).Row(r =>
                        {
                            r.RelativeItem().Text("Total:").Bold().FontSize(12);
                            r.ConstantItem(90).AlignRight().Text($"Rs. {invoice.TotalAmount:N2}").Bold().FontSize(12);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Thank you for your business - ");
                    text.Span("AutoParts Pro").SemiBold();
                });
            });
        });
        var pdf = document.GeneratePdf();

        if (!IsValidPdf(pdf))
            throw new InvalidOperationException("Failed to generate a valid invoice PDF.");

        return pdf;
    }
}
