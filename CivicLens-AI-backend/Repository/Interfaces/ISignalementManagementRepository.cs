using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Repository.Interfaces
{
    public interface ISignalementRepository
    {
        Task<Signalement?> GetByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct);
        Task<List<Signalement>> GetListAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<List<Signalement>> GetFilteredListAsync(
            Domaine? domaine,
            string delegation,
            StatutSignalement? statut,
            Gravite? gravite,
            Priorite? priorite,
            DateTime? dateResolutionFrom,
            DateTime? dateResolutionTo,
            CancellationToken ct);
        Task<Signalement?> GetOrganisationByIdAsync(long id, Guid organisationId, CancellationToken ct);
        Task<List<Signalement>> GetOrganisationListAsync(
            Guid organisationId,
            StatutSignalement? statut,
            Gravite? gravite,
            Priorite? priorite,
            DateTime? dateResolutionFrom,
            DateTime? dateResolutionTo,
            CancellationToken ct);
        Task<Dictionary<StatutSignalement, int>> CountByStatusAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<string?> GetCinByCitoyenIdAsync(Guid citoyenId, CancellationToken ct);
        Task<bool> SaveAsync(CancellationToken ct);
    }
}
