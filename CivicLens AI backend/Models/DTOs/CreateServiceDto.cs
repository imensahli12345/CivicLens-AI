namespace CivicLens.API.Models.DTOs
{
    public class CreateServiceDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Domaine { get; set; } = string.Empty;       // enum name: ROUTE, EAU, etc.
        public Guid BureauDeControleId { get; set; }
    }
}
