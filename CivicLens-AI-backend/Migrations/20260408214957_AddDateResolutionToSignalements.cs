using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class AddDateResolutionToSignalements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateResolution",
                table: "Signalements",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE ""Signalements""
SET ""DateResolution"" = ""DateCreation""
WHERE ""DateResolution"" IS NULL
  AND ""Statut"" = 'RESOLU';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateResolution",
                table: "Signalements");
        }
    }
}
