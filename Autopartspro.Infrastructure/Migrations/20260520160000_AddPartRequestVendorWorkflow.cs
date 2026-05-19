using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520160000_AddPartRequestVendorWorkflow")]
public partial class AddPartRequestVendorWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "InvoiceRecordedAt",
            table: "PartRequests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PurchaseInvoiceId",
            table: "PartRequests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "VendorId",
            table: "PartRequests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "VendorRequestMessage",
            table: "PartRequests",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "VendorRequestedAt",
            table: "PartRequests",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_PartRequests_PurchaseInvoiceId",
            table: "PartRequests",
            column: "PurchaseInvoiceId");

        migrationBuilder.CreateIndex(
            name: "IX_PartRequests_VendorId",
            table: "PartRequests",
            column: "VendorId");

        migrationBuilder.AddForeignKey(
            name: "FK_PartRequests_PurchaseInvoices_PurchaseInvoiceId",
            table: "PartRequests",
            column: "PurchaseInvoiceId",
            principalTable: "PurchaseInvoices",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_PartRequests_Vendors_VendorId",
            table: "PartRequests",
            column: "VendorId",
            principalTable: "Vendors",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_PartRequests_PurchaseInvoices_PurchaseInvoiceId", table: "PartRequests");
        migrationBuilder.DropForeignKey(name: "FK_PartRequests_Vendors_VendorId", table: "PartRequests");
        migrationBuilder.DropIndex(name: "IX_PartRequests_PurchaseInvoiceId", table: "PartRequests");
        migrationBuilder.DropIndex(name: "IX_PartRequests_VendorId", table: "PartRequests");
        migrationBuilder.DropColumn(name: "InvoiceRecordedAt", table: "PartRequests");
        migrationBuilder.DropColumn(name: "PurchaseInvoiceId", table: "PartRequests");
        migrationBuilder.DropColumn(name: "VendorId", table: "PartRequests");
        migrationBuilder.DropColumn(name: "VendorRequestMessage", table: "PartRequests");
        migrationBuilder.DropColumn(name: "VendorRequestedAt", table: "PartRequests");
    }
}
