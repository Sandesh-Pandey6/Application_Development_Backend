using Autopartspro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Autopartspro.Infrastructure.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260520120000_AddAppointmentReschedule")]
public partial class AddAppointmentReschedule : Migration{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateOnly>(
            name: "ProposedDate",
            table: "Appointments",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<TimeOnly>(
            name: "ProposedTime",
            table: "Appointments",
            type: "time without time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "StaffNotes",
            table: "Appointments",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "ProposedDate", table: "Appointments");
        migrationBuilder.DropColumn(name: "ProposedTime", table: "Appointments");
        migrationBuilder.DropColumn(name: "StaffNotes", table: "Appointments");
    }
}
