using CivicLens.API.Models.Entities;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;

namespace CivicLens.API.Services.Classes
{
    public class HistoriqueService : IHistoriqueService
    {
        private readonly IHistoriqueRepository _repository;

        public HistoriqueService(IHistoriqueRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<HistoriqueStatut>> GetBySignalementIdAsync(long signalementId, CancellationToken ct)
            => await _repository.GetBySignalementIdAsync(signalementId, ct);

        public async Task<List<HistoriqueEntryDto>> GetExpandedBySignalementIdAsync(long signalementId, CancellationToken ct)
            => await _repository.GetExpandedBySignalementIdAsync(signalementId, ct);

        public async Task<HistoriqueStatut?> GetSingleBySignalementIdAsync(long signalementId, CancellationToken ct)
            => await _repository.GetSingleBySignalementIdAsync(signalementId, ct);

        public void Add(long signalementId, StatutSignalement statut, Guid actorId)
        {
            _repository.Add(new HistoriqueStatut
            {
                SignalementId   = signalementId,
                Statuts         = [statut],
                IdModificateurs = [actorId],
                DateChangement  = DateTime.UtcNow
            });
        }

        public async Task<bool> SaveAsync(CancellationToken ct)
            => await _repository.SaveAsync(ct);
    }
}
