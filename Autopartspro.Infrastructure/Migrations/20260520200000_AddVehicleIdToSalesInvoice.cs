using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[Migration("20260520200000_AddVehicleIdToSalesInvoice")]
public partial class AddVehicleIdToSalesInvoice : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "VehicleId",
            table: "SalesInvoices",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_SalesInvoices_VehicleId",
            table: "SalesInvoices",
            column: "VehicleId");

        migrationBuilder.AddForeignKey(
            name: "FK_SalesInvoices_Vehicles_VehicleId",
            table: "SalesInvoices",
            column: "VehicleId",
            principalTable: "Vehicles",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SalesInvoices_Vehicles_VehicleId",
            table: "SalesInvoices");

        migrationBuilder.DropIndex(
            name: "IX_SalesInvoices_VehicleId",
            table: "SalesInvoices");

        migrationBuilder.DropColumn(
            name: "VehicleId",
            table: "SalesInvoices");
    }
}
