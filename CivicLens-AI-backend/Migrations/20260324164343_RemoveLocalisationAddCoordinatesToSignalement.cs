using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocalisationAddCoordinatesToSignalement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Signalements",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Signalements",
                type: "double precision",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Signalements" AS s
                SET "Latitude" = l."Latitude",
                    "Longitude" = l."Longitude"
                FROM "Localisations" AS l
                WHERE l."SignalementId" = s."Id";
                """);

            migrationBuilder.DropTable(
                name: "Localisations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Signalements");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Signalements");

            migrationBuilder.CreateTable(
                name: "Localisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalementId = table.Column<long>(type: "bigint", nullable: false),
                    AdresseTexte = table.Column<string>(type: "text", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Localisations_Signalements_SignalementId",
                        column: x => x.SignalementId,
                        principalTable: "Signalements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Localisations_SignalementId",
                table: "Localisations",
                column: "SignalementId",
                unique: true);
        }
    }
}
