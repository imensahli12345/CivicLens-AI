namespace CivicLens.API.Models.DTOs
{
    public class CreateBureauDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}
