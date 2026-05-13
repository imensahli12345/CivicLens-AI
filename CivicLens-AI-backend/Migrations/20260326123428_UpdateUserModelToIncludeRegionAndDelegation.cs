using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserModelToIncludeRegionAndDelegation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Region",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Organisations");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "BureauxDeControle");

            migrationBuilder.RenameColumn(
                name: "Adresse",
                table: "Users",
                newName: "Region");

            migrationBuilder.AddColumn<string>(
                name: "Delegation",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Delegation",
                table: "Users");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "Users",
                newName: "Adresse");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Organisations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "BureauxDeControle",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
