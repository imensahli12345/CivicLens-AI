namespace CivicLens.API.Models.DTOs
{
    public class SendSignalementDto
    {
        /// <summary>
        /// ID de l'organisation cible. Doit correspondre au même domaine que le signalement.
        /// </summary>
        public Guid OrganisationId { get; set; }

        /// <summary>
        /// Description supplémentaire ajoutée par le Service/Bureau au moment de l'envoi (optionnelle).
        /// </summary>
        public string? DescriptionSupplementaire { get; set; }
    }
}
