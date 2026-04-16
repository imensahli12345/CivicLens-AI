using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceRegionWithAdresseJSON : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "Signalements");

            migrationBuilder.AddColumn<string>(
                name: "Adresse",
                table: "Signalements",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Adresse",
                table: "Signalements");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Signalements",
                type: "text",
                nullable: true);
        }
    }
}
