using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOrphanServicesIntoServicesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""Services"" (""Id"", ""Nom"", ""Domaine"", ""BureauDeControleId"")
                SELECT 
                    u.""Id"",
                    'Service ' || (CASE WHEN POSITION('.' IN u.""Email"") > 0 THEN UPPER(SUBSTRING(u.""Email"" FROM 1 FOR POSITION('.' IN u.""Email"")-1)) ELSE 'ROUTE' END), 
                    (CASE WHEN POSITION('.' IN u.""Email"") > 0 THEN UPPER(SUBSTRING(u.""Email"" FROM 1 FOR POSITION('.' IN u.""Email"")-1)) ELSE 'ROUTE' END),
                    b.""Id""
                FROM ""Users"" u
                LEFT JOIN ""Users"" ub ON ub.""Delegation"" = u.""Delegation"" AND ub.""Role"" = 'BUREAUDECONTROLE'
                LEFT JOIN ""BureauxDeControle"" b ON b.""Id"" = ub.""Id""
                WHERE u.""Role"" = 'SERVICE'
                AND NOT EXISTS (
                    SELECT 1 FROM ""Services"" s WHERE s.""Id"" = u.""Id""
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Can't reliably rollback a data fix without deleting the backfilled ones.
            // We just leave it empty.
        }
    }
}
