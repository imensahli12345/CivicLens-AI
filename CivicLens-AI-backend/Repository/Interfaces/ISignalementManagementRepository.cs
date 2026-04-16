using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Repository.Interfaces
{
    public interface ISignalementRepository
{
    Task<Signalement?> GetByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct);
    Task<List<Signalement>> GetListAsync(Domaine? domaine, string delegation, CancellationToken ct);
    Task<Dictionary<StatutSignalement, int>> CountByStatusAsync(Domaine? domaine, string delegation, CancellationToken ct);
}
}
