using System;

namespace CivicLens.API.Models.DTOs
{
    public class ParticipationDto
    {
        public Guid Id { get; set; }
        public DateTime DateParticipation { get; set; }
        public string Commentaire { get; set; } = string.Empty;
        public bool PresenceConfirmee { get; set; }
        public Guid CitoyenId { get; set; }
        public string CitoyenNom { get; set; } = string.Empty;
        public Guid EvenementId { get; set; }
    }
}
