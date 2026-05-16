using System;

namespace CivicLens.API.Models.Entities
{
    public class PlatformReclamation
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? FromUserId { get; set; }
        public string FromEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? AppVersion { get; set; }
        public string? Context { get; set; }
        public string Status { get; set; } = "NOUVELLE";
    }
}
