using CivicLens.API.Models.Entities;
using CivicLens.API.Models.DTOs;

namespace CivicLens.API.Repository.Interfaces
{
    public interface IHistoriqueRepository
    {
        Task<List<HistoriqueStatut>> GetBySignalementIdAsync(long signalementId, CancellationToken ct);
        Task<List<HistoriqueEntryDto>> GetExpandedBySignalementIdAsync(long signalementId, CancellationToken ct);
        Task<HistoriqueStatut?> GetSingleBySignalementIdAsync(long signalementId, CancellationToken ct);
        void Add(HistoriqueStatut historique);
        Task<bool> SaveAsync(CancellationToken ct);
    }
}
