using System;

namespace CivicLens.API.Models.Entities
{
    public class Vote
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime DateVote { get; set; } = DateTime.UtcNow;

        // Foreign Keys
        public Guid CitoyenId { get; set; }
        public long SignalementId { get; set; }

        // Navigation Properties
        public Citoyen Citoyen { get; set; } = null!;
        public Signalement Signalement { get; set; } = null!;
    }
}