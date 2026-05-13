using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Services.Interfaces
{
    public interface IOrganisationService
    {
        Task<Organisation?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<Organisation?> GetFirstAsync(CancellationToken ct);
        Task<List<Organisation>> GetListAsync(Domaine? domaine, CancellationToken ct);
        Task<string?> GetNameByIdAsync(Guid id, CancellationToken ct);
    }
}
