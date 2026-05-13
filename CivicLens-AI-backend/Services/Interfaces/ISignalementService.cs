using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Services.Interfaces
{
    public interface ISignalementService
    {
        Task<List<Signalement>> GetListAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<Signalement?> GetByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct);
        Task<SignalementStatsDto> GetStatsAsync(Domaine? domaine, string delegation, CancellationToken ct);
    }
}
