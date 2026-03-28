using System;

namespace CivicLens.API.Models.Entities
{
    public class Notification
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Foreign Key
        public Guid CitoyenId { get; set; }

        // Navigation
        public Citoyen Citoyen { get; set; } = null!;

        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public Guid? RelatedEvenementId { get; set; }

        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

