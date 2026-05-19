using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520140000_AddPartRequestEscalation")]
public partial class AddPartRequestEscalation : Migration{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "EscalatedAt",
            table: "PartRequests",
            type: "timestamp with time zone",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "EscalatedAt", table: "PartRequests");
    }
}
