
using CivicLens.API.Models.Enums;


namespace CivicLens.API.Models.Entities
{
    public class HistoriqueStatut
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public StatutSignalement AncienStatut { get; set; }
        public StatutSignalement NouveauStatut { get; set; }
        public DateTime DateChangement { get; set; } = DateTime.UtcNow;
        public Guid IdModificateur { get; set; }

        // Foreign Key
        public long SignalementId { get; set; }

        // Navigation Property
        public Signalement Signalement { get; set; } = null!;
    }
}