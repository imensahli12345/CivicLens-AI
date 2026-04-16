using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Services.Interfaces
{
    public interface ISignalementManagementService
    {
        // ── Requêtes ──────────────────────────────────────────────────────────
        Task<List<Signalement>> GetSignalementsAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<Signalement?> GetSignalementByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct);
        Task<List<HistoriqueStatut>> GetHistoriquesAsync(long signalementId, CancellationToken ct);
        Task<Organisation?> GetOrganisationByIdAsync(Guid id, CancellationToken ct);
        Task<List<Organisation>> GetOrganisationsAsync(Domaine? domaine, CancellationToken ct);
        Task<SignalementStatsDto> GetStatsAsync(Domaine? domaine, string delegation, CancellationToken ct);

        // ── Transitions de statut ─────────────────────────────────────────────
        /// <returns>false si le statut actuel n'est pas EN_ATTENTE.</returns>
        Task<bool> OuvrirSignalementAsync(Signalement signalement, Guid actorId, CancellationToken ct);

        /// <returns>false si le statut actuel n'est pas OUVERTE.</returns>
        Task<bool> EnvoyerSignalementAsync(Signalement signalement, Guid actorId, Organisation organisation, string? descriptionSupp, CancellationToken ct);

        /// <returns>false si le statut actuel n'est pas OUVERTE.</returns>
        Task<bool> RejeterSignalementAsync(Signalement signalement, Guid actorId, CancellationToken ct);
    }
}
