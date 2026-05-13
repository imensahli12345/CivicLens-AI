using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class FixHistoriqueStatutDuplicateStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
WITH expanded AS (
    SELECT h.""Id"" AS hid, x.value::text AS statut, x.ordinality AS ord
    FROM ""HistoriquesStatut"" h
    CROSS JOIN LATERAL jsonb_array_elements_text(h.""Statuts"") WITH ORDINALITY AS x(value, ordinality)
),
flagged AS (
    SELECT
        hid,
        statut,
        ord,
        lag(statut) OVER (PARTITION BY hid ORDER BY ord) AS prev_statut
    FROM expanded
),
dedup AS (
    SELECT
        hid,
        jsonb_agg(statut ORDER BY ord) AS new_statuts
    FROM flagged
    WHERE prev_statut IS DISTINCT FROM statut
    GROUP BY hid
)
UPDATE ""HistoriquesStatut"" h
SET ""Statuts"" = d.new_statuts
FROM dedup d
WHERE h.""Id"" = d.hid;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
