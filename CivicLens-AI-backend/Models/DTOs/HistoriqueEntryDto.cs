namespace CivicLens.API.Models.DTOs
{
    public class HistoriqueEntryDto
    {
        public Guid Id { get; set; }
        public DateTime DateChangement { get; set; }
        public string Statut { get; set; } = string.Empty;
        public Guid? ModificateurId { get; set; }
        public string? Modificateur { get; set; }
    }
}
