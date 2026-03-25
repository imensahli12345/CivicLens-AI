using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    public class Citoyen : User
    {
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Cin { get; set; } = string.Empty;

        // Navigation Properties spécifiques au Citoyen
        public ICollection<Signalement> Signalements { get; set; } = new List<Signalement>();
        public ICollection<Evenement> Evenements { get; set; } = new List<Evenement>();
        public ICollection<Participation> Participations { get; set; } = new List<Participation>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    }
}