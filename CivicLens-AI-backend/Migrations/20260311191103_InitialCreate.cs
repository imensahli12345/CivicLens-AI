using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CivicLens_AI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    MotDePasseHash = table.Column<string>(type: "text", nullable: false),
                    Adresse = table.Column<string>(type: "text", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DernierLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatutCompte = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BureauxDeControle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Matricule = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BureauxDeControle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BureauxDeControle_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Citoyens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Prenom = table.Column<string>(type: "text", nullable: false),
                    Cin = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citoyens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citoyens_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Organisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NomOrganisation = table.Column<string>(type: "text", nullable: false),
                    TypeOrganisation = table.Column<string>(type: "text", nullable: false),
                    DomaineOrganisation = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Organisations_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Nom = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    Domaine = table.Column<string>(type: "text", nullable: false),
                    BureauDeControleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Services_BureauxDeControle_BureauDeControleId",
                        column: x => x.BureauDeControleId,
                        principalTable: "BureauxDeControle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Services_Users_Id",
                        column: x => x.Id,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Evenements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Titre = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateEvenement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NombreVolontairesRequis = table.Column<int>(type: "integer", nullable: false),
                    OrganisateurId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evenements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Evenements_Citoyens_OrganisateurId",
                        column: x => x.OrganisateurId,
                        principalTable: "Citoyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Signalements",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DateCreation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Statut = table.Column<string>(type: "text", nullable: false),
                    Gravite = table.Column<string>(type: "text", nullable: false),
                    Priorite = table.Column<string>(type: "text", nullable: false),
                    Domaine = table.Column<string>(type: "text", nullable: false),
                    ScoreAI = table.Column<double>(type: "double precision", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: false),
                    CitoyenId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganisationId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signalements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Signalements_Citoyens_CitoyenId",
                        column: x => x.CitoyenId,
                        principalTable: "Citoyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Signalements_Organisations_OrganisationId",
                        column: x => x.OrganisationId,
                        principalTable: "Organisations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Participations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateParticipation = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Commentaire = table.Column<string>(type: "text", nullable: false),
                    PresenceConfirmee = table.Column<bool>(type: "boolean", nullable: false),
                    CitoyenId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvenementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Participations_Citoyens_CitoyenId",
                        column: x => x.CitoyenId,
                        principalTable: "Citoyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Participations_Evenements_EvenementId",
                        column: x => x.EvenementId,
                        principalTable: "Evenements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HistoriquesStatut",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AncienStatut = table.Column<string>(type: "text", nullable: false),
                    NouveauStatut = table.Column<string>(type: "text", nullable: false),
                    DateChangement = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IdModificateur = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalementId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoriquesStatut", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistoriquesStatut_Signalements_SignalementId",
                        column: x => x.SignalementId,
                        principalTable: "Signalements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ImagesSignalement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UrlImage = table.Column<string>(type: "text", nullable: false),
                    DateAjout = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignalementId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImagesSignalement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ImagesSignalement_Signalements_SignalementId",
                        column: x => x.SignalementId,
                        principalTable: "Signalements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Localisations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    AdresseTexte = table.Column<string>(type: "text", nullable: false),
                    SignalementId = table.Column<long>(type: "bigint", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DateVote = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CitoyenId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalementId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Votes_Citoyens_CitoyenId",
                        column: x => x.CitoyenId,
                        principalTable: "Citoyens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Votes_Signalements_SignalementId",
                        column: x => x.SignalementId,
                        principalTable: "Signalements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BureauxDeControle_Matricule",
                table: "BureauxDeControle",
                column: "Matricule",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citoyens_Cin",
                table: "Citoyens",
                column: "Cin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Evenements_OrganisateurId",
                table: "Evenements",
                column: "OrganisateurId");

            migrationBuilder.CreateIndex(
                name: "IX_HistoriquesStatut_SignalementId",
                table: "HistoriquesStatut",
                column: "SignalementId");

            migrationBuilder.CreateIndex(
                name: "IX_ImagesSignalement_SignalementId",
                table: "ImagesSignalement",
                column: "SignalementId");

            migrationBuilder.CreateIndex(
                name: "IX_Localisations_SignalementId",
                table: "Localisations",
                column: "SignalementId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participations_CitoyenId",
                table: "Participations",
                column: "CitoyenId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_EvenementId",
                table: "Participations",
                column: "EvenementId");

            migrationBuilder.CreateIndex(
                name: "IX_Services_BureauDeControleId",
                table: "Services",
                column: "BureauDeControleId");

            migrationBuilder.CreateIndex(
                name: "IX_Signalements_CitoyenId",
                table: "Signalements",
                column: "CitoyenId");

            migrationBuilder.CreateIndex(
                name: "IX_Signalements_OrganisationId",
                table: "Signalements",
                column: "OrganisationId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_CitoyenId_SignalementId",
                table: "Votes",
                columns: new[] { "CitoyenId", "SignalementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Votes_SignalementId",
                table: "Votes",
                column: "SignalementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistoriquesStatut");

            migrationBuilder.DropTable(
                name: "ImagesSignalement");

            migrationBuilder.DropTable(
                name: "Localisations");

            migrationBuilder.DropTable(
                name: "Participations");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "Votes");

            migrationBuilder.DropTable(
                name: "Evenements");

            migrationBuilder.DropTable(
                name: "BureauxDeControle");

            migrationBuilder.DropTable(
                name: "Signalements");

            migrationBuilder.DropTable(
                name: "Citoyens");

            migrationBuilder.DropTable(
                name: "Organisations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
