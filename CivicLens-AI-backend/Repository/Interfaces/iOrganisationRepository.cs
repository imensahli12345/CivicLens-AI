using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Repository.Interfaces
{
    public interface IOrganisationRepository
    {
        Task<Organisation?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<Organisation>> GetListAsync(Domaine? domaine, CancellationToken ct);
    }
}