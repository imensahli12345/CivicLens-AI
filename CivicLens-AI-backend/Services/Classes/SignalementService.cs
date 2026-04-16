using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;

namespace CivicLens.API.Services.Classes
{
    public class SignalementService : ISignalementService
    {
        private readonly ISignalementRepository _repository;

        public SignalementService(ISignalementRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<Signalement>> GetListAsync(Domaine? domaine, string delegation, CancellationToken ct)
            => await _repository.GetListAsync(domaine, delegation, ct);

        public async Task<Signalement?> GetByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct)
            => await _repository.GetByIdAsync(id, domaine, delegation, ct);

        public async Task<SignalementStatsDto> GetStatsAsync(Domaine? domaine, string delegation, CancellationToken ct)
        {
            var counts = await _repository.CountByStatusAsync(domaine, delegation, ct);
            return new SignalementStatsDto
            {
                Total         = counts.Values.Sum(),
                EnAttente     = counts.GetValueOrDefault(StatutSignalement.EN_ATTENTE),
                Ouvertes      = counts.GetValueOrDefault(StatutSignalement.OUVERTE),
                Envoyees      = counts.GetValueOrDefault(StatutSignalement.ENVOYEE),
                EnCoursDEtude = counts.GetValueOrDefault(StatutSignalement.EN_COURS_DETUDE),
                Resolus       = counts.GetValueOrDefault(StatutSignalement.RESOLU),
                Rejetes       = counts.GetValueOrDefault(StatutSignalement.REJETE),
                Domaine       = domaine?.ToString(),
                Delegation    = delegation
            };
        }
    }
}
