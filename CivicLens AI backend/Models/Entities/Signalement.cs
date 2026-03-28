using CivicLens.API.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace CivicLens.API.Models.Entities
{
    public class Signalement
    {
        public long Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public StatutSignalement Statut { get; set; } = StatutSignalement.EN_ATTENTE;
        public Gravite? Gravite { get; set; }
        public Priorite? Priorite { get; set; }
        public Domaine? Domaine { get; set; }
        public double? ScoreAI { get; set; }
        
        [Column(TypeName = "jsonb")]
        public string? Adresse { get; set; }
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // Foreign Keys
        public Guid CitoyenId { get; set; }
        public Guid? OrganisationId { get; set; }

        // Navigation Properties
        public Citoyen Citoyen { get; set; } = null!;
        public Organisation? Organisation { get; set; }
        public ICollection<ImageSignalement> Images { get; set; } = new List<ImageSignalement>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<HistoriqueStatut> Historiques { get; set; } = new List<HistoriqueStatut>();
    }
}
