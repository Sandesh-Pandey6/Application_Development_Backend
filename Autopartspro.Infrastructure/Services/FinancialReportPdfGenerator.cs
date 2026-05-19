using Autopartspro.Application.Dtos.Admin;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Autopartspro.Infrastructure.Services;

public static class FinancialReportPdfGenerator
{
    public static byte[] Build(FinancialReportDto report, string periodLabel, string periodRange)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text("AutoParts Pro").FontSize(20).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().Text("Financial Report").FontSize(14).SemiBold();
                    col.Item().Text($"{periodLabel} — {periodRange}").FontColor(Colors.Grey.Darken1);
                    col.Item().Text($"Generated: {DateTime.UtcNow:dd MMM yyyy HH:mm} UTC").FontSize(8)
                        .FontColor(Colors.Grey.Medium);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Item().Text("Summary").FontSize(12).Bold();
                    col.Item().PaddingTop(6).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2);
                            c.RelativeColumn(1);
                            c.RelativeColumn(1);
                        });

                        static IContainer Cell(IContainer c, bool header = false) =>
                            c.Padding(6).Background(header ? Colors.Grey.Lighten3 : Colors.White);

                        table.Cell().Element(c => Cell(c, true)).Text("Metric").SemiBold();
                        table.Cell().Element(c => Cell(c, true)).AlignRight().Text("Amount / %").SemiBold();
                        table.Cell().Element(c => Cell(c, true)).AlignRight().Text("Change vs prev.").SemiBold();

                        table.Cell().Element(c => Cell(c)).Text("Gross revenue");
                        table.Cell().Element(c => Cell(c)).AlignRight().Text($"Rs. {report.GrossRevenue:N2}");
                        table.Cell().Element(c => Cell(c)).AlignRight()
                            .Text($"{report.RevenueChangePercent:+#0.0;-#0.0;0}%");

                        table.Cell().Element(c => Cell(c)).Text("Total expenses");
                        table.Cell().Element(c => Cell(c)).AlignRight().Text($"Rs. {report.TotalExpenses:N2}");
                        table.Cell().Element(c => Cell(c)).AlignRight()
                            .Text($"{report.ExpensesChangePercent:+#0.0;-#0.0;0}%");

                        table.Cell().Element(c => Cell(c)).Text("Net margin");
                        table.Cell().Element(c => Cell(c)).AlignRight().Text($"{report.NetMargin:N1}%");
                        table.Cell().Element(c => Cell(c)).AlignRight()
                            .Text($"{report.NetMarginChangePercent:+#0.0;-#0.0;0} pts");
                    });

                    if (report.SalesRevenueOverTime.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Sales revenue over time").FontSize(11).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                            });
                            table.Cell().Element(HeaderCell).Text("Period");
                            table.Cell().Element(HeaderCell).AlignRight().Text("Revenue (Rs.)");
                            foreach (var row in report.SalesRevenueOverTime)
                            {
                                table.Cell().Element(BodyCell).Text(row.Label);
                                table.Cell().Element(BodyCell).AlignRight().Text(row.Revenue.ToString("N2"));
                            }
                        });
                    }

                    if (report.PurchaseCostsByCategory.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Purchase costs by category").FontSize(11).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                            });
                            table.Cell().Element(HeaderCell).Text("Category");
                            table.Cell().Element(HeaderCell).AlignRight().Text("Cost (Rs.)");
                            foreach (var row in report.PurchaseCostsByCategory)
                            {
                                table.Cell().Element(BodyCell).Text(row.Category);
                                table.Cell().Element(BodyCell).AlignRight().Text(row.TotalCost.ToString("N2"));
                            }
                        });
                    }

                    if (report.TopSellingParts.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Top selling parts").FontSize(11).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.ConstantColumn(50);
                                c.RelativeColumn(1);
                            });
                            table.Cell().Element(HeaderCell).Text("Part");
                            table.Cell().Element(HeaderCell).Text("Category");
                            table.Cell().Element(HeaderCell).AlignCenter().Text("Qty");
                            table.Cell().Element(HeaderCell).AlignRight().Text("Revenue");
                            foreach (var row in report.TopSellingParts)
                            {
                                table.Cell().Element(BodyCell).Text(row.PartName);
                                table.Cell().Element(BodyCell).Text(row.Category);
                                table.Cell().Element(BodyCell).AlignCenter().Text(row.UnitsSold.ToString());
                                table.Cell().Element(BodyCell).AlignRight().Text($"Rs. {row.TotalRevenue:N2}");
                            }
                        });
                    }

                    if (report.VendorSpend.Count > 0)
                    {
                        col.Item().PaddingTop(16).Text("Vendor spend").FontSize(11).Bold();
                        col.Item().PaddingTop(4).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.ConstantColumn(55);
                            });
                            table.Cell().Element(HeaderCell).Text("Vendor");
                            table.Cell().Element(HeaderCell).AlignRight().Text("Spend (Rs.)");
                            table.Cell().Element(HeaderCell).AlignRight().Text("%");
                            foreach (var row in report.VendorSpend)
                            {
                                table.Cell().Element(BodyCell).Text(row.VendorName);
                                table.Cell().Element(BodyCell).AlignRight().Text(row.TotalSpend.ToString("N2"));
                                table.Cell().Element(BodyCell).AlignRight().Text($"{row.Percentage:N1}%");
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static IContainer HeaderCell(IContainer c) =>
        c.DefaultTextStyle(x => x.SemiBold()).Background(Colors.Grey.Lighten3).Padding(5);

    private static IContainer BodyCell(IContainer c) =>
        c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
