namespace CivicLens.API.Models.Entities
{
    public class ImageSignalement
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UrlImage { get; set; } = string.Empty;
        public DateTime DateAjout { get; set; } = DateTime.UtcNow;

        // Foreign Key
        public long SignalementId { get; set; }

        // Navigation Property
        public Signalement Signalement { get; set; } = null!;
    }
}
