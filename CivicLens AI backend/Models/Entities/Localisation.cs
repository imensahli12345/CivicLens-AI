using System;

namespace CivicLens.API.Models.Entities
{
    public class Localisation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string AdresseTexte { get; set; } = string.Empty;

        public long SignalementId { get; set; }

        // Navigation Property
        public Signalement Signalement { get; set; } = null!;
    }
}
