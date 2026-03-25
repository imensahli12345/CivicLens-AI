using System;

namespace CivicLens.API.Models.Entities
{
    public class Participation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DateParticipation { get; set; } = DateTime.UtcNow;
        public string Commentaire { get; set; } = string.Empty;
        public bool PresenceConfirmee { get; set; } = false;

        // Foreign Keys
        public Guid CitoyenId { get; set; }
        public Guid EvenementId { get; set; }

        // Navigation Properties
        public Citoyen Citoyen { get; set; } = null!;
        public Evenement Evenement { get; set; } = null!;
    }
}