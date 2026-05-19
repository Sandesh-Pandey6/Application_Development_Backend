using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520150000_AddBusinessEmailToUser")]
public partial class AddBusinessEmailToUser : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "BusinessEmail",
            table: "Users",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "BusinessEmail", table: "Users");
    }
}
