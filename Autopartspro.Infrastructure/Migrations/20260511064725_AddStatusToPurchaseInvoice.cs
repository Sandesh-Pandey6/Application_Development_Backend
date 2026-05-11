using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToPurchaseInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PurchaseInvoices",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PurchaseInvoices");
        }
    }
}
