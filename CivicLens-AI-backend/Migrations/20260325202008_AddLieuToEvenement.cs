using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddLieuToEvenement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Lieu",
                table: "Evenements",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Lieu",
                table: "Evenements");
        }
    }
}
