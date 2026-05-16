using System;

namespace CivicLens.API.Models.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CitoyenId { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public Guid? RelatedEvenementId { get; set; }
        //new code so we can save the signalement related to the notification
        public long? RelatedSignalementId { get; set; }
        public Guid? RelatedReclamationId { get; set; }
        public Guid? RelatedUserId { get; set; }
        public string? ActionToken { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

