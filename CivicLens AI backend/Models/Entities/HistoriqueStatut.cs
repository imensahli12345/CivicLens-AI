
using CivicLens.API.Models.Enums;


namespace CivicLens.API.Models.Entities
{
    public class HistoriqueStatut
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<StatutSignalement> Statuts { get; set; } = new();
        public DateTime DateChangement { get; set; } = DateTime.UtcNow;
        public List<Guid> IdModificateurs { get; set; } = new();

        // Foreign Key
        public long SignalementId { get; set; }

        // Navigation Property
        public Signalement Signalement { get; set; } = null!;
    }
}
