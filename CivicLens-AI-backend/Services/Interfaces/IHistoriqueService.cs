using CivicLens.API.Models.Entities;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Services.Interfaces
{
    public interface IHistoriqueService
    {
        Task<List<HistoriqueStatut>> GetBySignalementIdAsync(long signalementId, CancellationToken ct);
        Task<List<HistoriqueEntryDto>> GetExpandedBySignalementIdAsync(long signalementId, CancellationToken ct);
        Task<HistoriqueStatut?> GetSingleBySignalementIdAsync(long signalementId, CancellationToken ct);

        /// <summary>
        /// Adds a new status transition to the signalement history.
        /// No save — call SaveAsync() when you're ready to commit everything at once.
        /// </summary>
        void Add(long signalementId, StatutSignalement statut, Guid actorId);

        /// <summary>
        /// Persists all tracked changes in the shared DbContext
        /// (signalement update + new historique + new notification — all in one SaveChanges).
        /// </summary>
        Task<bool> SaveAsync(CancellationToken ct);
    }
}
