using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class NotificationsRecipientUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Citoyens_CitoyenId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Users_CitoyenId",
                table: "Notifications",
                column: "CitoyenId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Users_CitoyenId",
                table: "Notifications");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Citoyens_CitoyenId",
                table: "Notifications",
                column: "CitoyenId",
                principalTable: "Citoyens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
