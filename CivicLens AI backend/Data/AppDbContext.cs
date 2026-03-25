using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CivicLens.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── DbSets ──
        public DbSet<User> Users { get; set; }
        public DbSet<Citoyen> Citoyens { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<BureauDeControle> BureauxDeControle { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Signalement> Signalements { get; set; }
        public DbSet<Localisation> Localisations { get; set; }
        public DbSet<ImageSignalement> ImagesSignalement { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<HistoriqueStatut> HistoriquesStatut { get; set; }
        public DbSet<Evenement> Evenements { get; set; }
        public DbSet<Participation> Participations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ── TPT Inheritance ──
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Citoyen>().ToTable("Citoyens");
            modelBuilder.Entity<Organisation>().ToTable("Organisations");
            modelBuilder.Entity<BureauDeControle>().ToTable("BureauxDeControle");
            modelBuilder.Entity<Service>().ToTable("Services");

            // ── Enums as strings ──
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.StatutCompte)
                .HasConversion<string>();

            modelBuilder.Entity<Organisation>()
                .Property(o => o.TypeOrganisation)
                .HasConversion<string>();

            modelBuilder.Entity<Organisation>()
                .Property(o => o.DomaineOrganisation)
                .HasConversion<string>();

            modelBuilder.Entity<Service>()
                .Property(s => s.Domaine)
                .HasConversion<string>();

            modelBuilder.Entity<Signalement>()
                .Property(s => s.Statut)
                .HasConversion<string>();

            modelBuilder.Entity<Signalement>()
                .Property(s => s.Gravite)
                .HasConversion<string>();

            modelBuilder.Entity<Signalement>()
                .Property(s => s.Priorite)
                .HasConversion<string>();

            modelBuilder.Entity<Signalement>()
                .Property(s => s.Domaine)
                .HasConversion<string>();

            modelBuilder.Entity<HistoriqueStatut>()
                .Property(h => h.AncienStatut)
                .HasConversion<string>();

            modelBuilder.Entity<HistoriqueStatut>()
                .Property(h => h.NouveauStatut)
                .HasConversion<string>();

            // ── Unique Constraints ──
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<Citoyen>()
                .HasIndex(c => c.Cin).IsUnique();

            modelBuilder.Entity<BureauDeControle>()
                .HasIndex(b => b.Matricule).IsUnique();

            modelBuilder.Entity<Vote>()
                .HasIndex(v => new { v.CitoyenId, v.SignalementId }).IsUnique();

            // ── Relations ──

            // Signalement → Localisation (1-1)
            modelBuilder.Entity<Signalement>()
                .HasOne(s => s.Localisation)
                .WithOne(l => l.Signalement)
                .HasForeignKey<Localisation>(l => l.SignalementId);

            // BureauDeControle → Services (1-many)
            modelBuilder.Entity<Service>()
                .HasOne(s => s.BureauDeControle)
                .WithMany(b => b.Services)
                .HasForeignKey(s => s.BureauDeControleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Citoyen → Signalements
            modelBuilder.Entity<Signalement>()
                .HasOne<Citoyen>(s => s.Citoyen)
                .WithMany(c => c.Signalements)
                .HasForeignKey(s => s.CitoyenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Signalement → Organisation
            modelBuilder.Entity<Signalement>()
                .HasOne(s => s.Organisation)
                .WithMany(o => o.Signalements)
                .HasForeignKey(s => s.OrganisationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Citoyen → Evenements
            modelBuilder.Entity<Evenement>()
                .HasOne<Citoyen>(e => e.Organisateur)
                .WithMany(c => c.Evenements)
                .HasForeignKey(e => e.OrganisateurId)
                .OnDelete(DeleteBehavior.Restrict);

            // Citoyen → Participations
            modelBuilder.Entity<Participation>()
                .HasOne<Citoyen>(p => p.Citoyen)
                .WithMany(c => c.Participations)
                .HasForeignKey(p => p.CitoyenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Evenement → Participations
            modelBuilder.Entity<Participation>()
                .HasOne(p => p.Evenement)
                .WithMany(e => e.Participations)
                .HasForeignKey(p => p.EvenementId)
                .OnDelete(DeleteBehavior.Restrict);

            // Vote → Citoyen
            modelBuilder.Entity<Vote>()
                .HasOne<Citoyen>(v => v.Citoyen)
                .WithMany(c => c.Votes)
                .HasForeignKey(v => v.CitoyenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Vote → Signalement
            modelBuilder.Entity<Vote>()
                .HasOne(v => v.Signalement)
                .WithMany(s => s.Votes)
                .HasForeignKey(v => v.SignalementId)
                .OnDelete(DeleteBehavior.Restrict);

            // HistoriqueStatut → Signalement
            modelBuilder.Entity<HistoriqueStatut>()
                .HasOne(h => h.Signalement)
                .WithMany(s => s.Historiques)
                .HasForeignKey(h => h.SignalementId)
                .OnDelete(DeleteBehavior.Restrict);

            // ImageSignalement → Signalement
            modelBuilder.Entity<ImageSignalement>()
                .HasOne(i => i.Signalement)
                .WithMany(s => s.Images)
                .HasForeignKey(i => i.SignalementId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }
    }
}
