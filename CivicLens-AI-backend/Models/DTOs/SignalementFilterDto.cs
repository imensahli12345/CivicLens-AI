using CivicLens.API.Models.Enums;

namespace CivicLens.API.Models.DTOs
{
    public class SignalementFilterDto
    {
        public string? Statut { get; set; }
        public string? Gravite { get; set; }
        public string? Priorite { get; set; }
        public DateTime? DateResolutionFrom { get; set; }
        public DateTime? DateResolutionTo { get; set; }

        public bool TryParse(
            out StatutSignalement? statut,
            out Gravite? gravite,
            out Priorite? priorite,
            out string? error)
        {
            statut = null;
            gravite = null;
            priorite = null;
            error = null;

            if (!string.IsNullOrWhiteSpace(Statut))
            {
                if (!Enum.TryParse<StatutSignalement>(Statut, true, out var parsedStatut))
                {
                    error = "Statut invalide.";
                    return false;
                }

                statut = parsedStatut;
            }

            if (!string.IsNullOrWhiteSpace(Gravite))
            {
                if (!Enum.TryParse<Gravite>(Gravite, true, out var parsedGravite))
                {
                    error = "Gravite invalide.";
                    return false;
                }

                gravite = parsedGravite;
            }

            if (!string.IsNullOrWhiteSpace(Priorite))
            {
                if (!Enum.TryParse<Priorite>(Priorite, true, out var parsedPriorite))
                {
                    error = "Priorite invalide.";
                    return false;
                }

                priorite = parsedPriorite;
            }

            if (DateResolutionFrom.HasValue &&
                DateResolutionTo.HasValue &&
                DateResolutionFrom.Value.Date > DateResolutionTo.Value.Date)
            {
                error = "DateResolutionFrom doit etre inferieure ou egale a DateResolutionTo.";
                return false;
            }

            return true;
        }
    }
}
