using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    public class Evenement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateEvenement { get; set; }
        public int NombreVolontairesRequis { get; set; }

        // Foreign Key
        public Guid OrganisateurId { get; set; }

        // Navigation Properties
        public Citoyen Organisateur { get; set; } = null!;
        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
    }
}