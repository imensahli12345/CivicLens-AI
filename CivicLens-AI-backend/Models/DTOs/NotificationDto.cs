using System;

namespace CivicLens.API.Models.DTOs
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Guid? RelatedEvenementId { get; set; }
        public long? RelatedSignalementId { get; set; }
        public Guid? RelatedReclamationId { get; set; }
        public Guid? RelatedUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}

