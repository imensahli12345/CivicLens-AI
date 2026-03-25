namespace CivicLens.API.Models.DTOs
{
    public class CreateAdminDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
    }
}
