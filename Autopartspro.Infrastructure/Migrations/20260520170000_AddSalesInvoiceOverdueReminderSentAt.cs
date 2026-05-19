using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[Migration("20260520170000_AddSalesInvoiceOverdueReminderSentAt")]
public partial class AddSalesInvoiceOverdueReminderSentAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "OverdueReminderSentAt",
            table: "SalesInvoices",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "OverdueReminderSentAt", table: "SalesInvoices");
    }
}
