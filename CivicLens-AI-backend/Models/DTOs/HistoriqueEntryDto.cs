namespace CivicLens.API.Models.DTOs
{
    public class HistoriqueEntryDto
    {
        public DateTime DateChangement { get; set; }
        public string Statut { get; set; } = string.Empty;
        public string? Modificateur { get; set; }
    }
}
