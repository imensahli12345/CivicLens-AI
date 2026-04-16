namespace CivicLens.API.Models.DTOs
{
    public class RegisterCitoyenDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Cin { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Delegation { get; set; } = string.Empty;
    }
}
