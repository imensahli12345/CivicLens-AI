namespace CivicLens.API.Models.DTOs
{
    public class CreateOrganisationDto
    {
        public string Email { get; set; } = string.Empty;
        public string MotDePasse { get; set; } = string.Empty;
        public string NomOrganisation { get; set; } = string.Empty;
        public string TypeOrganisation { get; set; } = string.Empty;
        public string DomaineOrganisation { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
    }
}