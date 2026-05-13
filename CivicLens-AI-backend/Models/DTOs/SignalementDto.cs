namespace CivicLens.API.Models.DTOs
{
    public class SignalementDto
    {
        public long Id { get; set; }

        public string Titre { get; set; }
        public string Description { get; set; }
        public string? DescriptionService { get; set; }
        public string? RapportUrl { get; set; }

        public DateTime DateCreation { get; set; }
        public DateTime? DateResolution { get; set; }

        public string Statut { get; set; }
        public string Gravite { get; set; }
        public string Priorite { get; set; }
        public string? Domaine { get; set; }

        public string? Adresse { get; set; }

        public string? CinCitoyen { get; set; }
        public string? OrganisationNom { get; set; }

        public List<string> Images { get; set; } = new();
    }
}