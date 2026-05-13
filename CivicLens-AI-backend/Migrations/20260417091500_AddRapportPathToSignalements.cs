using CivicLens.API.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260417091500_AddRapportPathToSignalements")]
    public partial class AddRapportPathToSignalements : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RapportPath",
                table: "Signalements",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RapportPath",
                table: "Signalements");
        }
    }
}
