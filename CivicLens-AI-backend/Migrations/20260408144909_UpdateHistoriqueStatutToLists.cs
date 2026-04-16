using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHistoriqueStatutToLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoriquesStatut_SignalementId",
                table: "HistoriquesStatut");

            migrationBuilder.AddColumn<string>(
                name: "IdModificateurs",
                table: "HistoriquesStatut",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.AddColumn<string>(
                name: "Statuts",
                table: "HistoriquesStatut",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.Sql(@"
WITH keep AS (
    SELECT DISTINCT ON (h.""SignalementId"") h.""SignalementId"", h.""Id""
    FROM ""HistoriquesStatut"" h
    ORDER BY h.""SignalementId"", h.""DateChangement""
)
DELETE FROM ""HistoriquesStatut"" h
USING keep k
WHERE h.""SignalementId"" = k.""SignalementId""
  AND h.""Id"" <> k.""Id"";
");

            migrationBuilder.Sql(@"
WITH keep AS (
    SELECT h.""SignalementId"", h.""Id""
    FROM ""HistoriquesStatut"" h
),
ordered AS (
    SELECT
        h.""SignalementId"",
        h.""DateChangement"",
        h.""AncienStatut""::text AS ancien,
        h.""NouveauStatut""::text AS nouveau,
        h.""IdModificateur"" AS modif,
        s.""CitoyenId"" AS owner
    FROM ""HistoriquesStatut"" h
    JOIN ""Signalements"" s ON s.""Id"" = h.""SignalementId""
),
agg AS (
    SELECT
        ""SignalementId"",
        (array_agg(ancien ORDER BY ""DateChangement""))[1] AS first_ancien,
        jsonb_agg(nouveau ORDER BY ""DateChangement"") AS nouveaux,
        jsonb_agg(modif ORDER BY ""DateChangement"") FILTER (WHERE modif <> owner) AS modifs,
        max(""DateChangement"") AS last_change
    FROM ordered
    GROUP BY ""SignalementId""
)
UPDATE ""HistoriquesStatut"" h
SET
    ""Statuts"" = jsonb_build_array(a.first_ancien) || a.nouveaux,
    ""IdModificateurs"" = COALESCE(a.modifs, '[]'::jsonb),
    ""DateChangement"" = a.last_change
FROM keep k
JOIN agg a ON a.""SignalementId"" = k.""SignalementId""
WHERE h.""Id"" = k.""Id"";
");

            migrationBuilder.DropColumn(
                name: "AncienStatut",
                table: "HistoriquesStatut");

            migrationBuilder.DropColumn(
                name: "IdModificateur",
                table: "HistoriquesStatut");

            migrationBuilder.DropColumn(
                name: "NouveauStatut",
                table: "HistoriquesStatut");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesStatut_SignalementId",
                table: "HistoriquesStatut",
                column: "SignalementId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HistoriquesStatut_SignalementId",
                table: "HistoriquesStatut");

            migrationBuilder.DropColumn(
                name: "IdModificateurs",
                table: "HistoriquesStatut");

            migrationBuilder.DropColumn(
                name: "Statuts",
                table: "HistoriquesStatut");

            migrationBuilder.AddColumn<string>(
                name: "AncienStatut",
                table: "HistoriquesStatut",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "IdModificateur",
                table: "HistoriquesStatut",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "NouveauStatut",
                table: "HistoriquesStatut",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesStatut_SignalementId",
                table: "HistoriquesStatut",
                column: "SignalementId");
        }
    }
}
