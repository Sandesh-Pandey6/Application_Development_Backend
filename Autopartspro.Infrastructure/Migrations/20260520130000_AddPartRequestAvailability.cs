using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520130000_AddPartRequestAvailability")]
public partial class AddPartRequestAvailability : Migration{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "EstimatedAvailableDate",
            table: "PartRequests",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StaffNotes",
            table: "PartRequests",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "StaffRespondedAt",
            table: "PartRequests",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EstimatedAvailableDate", table: "PartRequests");
        migrationBuilder.DropColumn(name: "StaffNotes", table: "PartRequests");
        migrationBuilder.DropColumn(name: "StaffRespondedAt", table: "PartRequests");
    }
}
