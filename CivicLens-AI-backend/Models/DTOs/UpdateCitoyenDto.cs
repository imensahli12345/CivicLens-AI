namespace CivicLens.API.Models.DTOs
{
    public class UpdateCitoyenDto
    {
        public string? Nom { get; set; }
        public string? Prenom { get; set; }
        public string? Region { get; set; }
        public string? Delegation { get; set; }
        public string? Telephone { get; set; }
        public string? PhotoUrl { get; set; }
    }
}
