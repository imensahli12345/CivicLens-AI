namespace CivicLens.API.Models.DTOs
{
    /// <summary>Statistiques des signalements pour le dashboard Service/Bureau.</summary>
    public class SignalementStatsDto
    {
        public int Total { get; set; }
        public int EnAttente { get; set; }
        public int Ouvertes { get; set; }
        public int Envoyees { get; set; }
        public int EnCoursDEtude { get; set; }
        public int Resolus { get; set; }
        public int Rejetes { get; set; }
        /// <summary>Domaine filtré (null = TOUS pour BureauDeControle).</summary>
        public string? Domaine { get; set; }
        public string Delegation { get; set; } = string.Empty;
    }
}
