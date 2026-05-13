using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionServiceAndNotificationSignalementId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionService",
                table: "Signalements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RelatedSignalementId",
                table: "Notifications",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionService",
                table: "Signalements");

            migrationBuilder.DropColumn(
                name: "RelatedSignalementId",
                table: "Notifications");
        }
    }
}
