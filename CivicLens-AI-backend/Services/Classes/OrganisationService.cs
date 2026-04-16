using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;

namespace CivicLens.API.Services.Classes
{
    public class OrganisationService : IOrganisationService
    {
        private readonly IOrganisationRepository _repository;

        public OrganisationService(IOrganisationRepository repository)
        {
            _repository = repository;
        }

        public async Task<Organisation?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _repository.GetByIdAsync(id, ct);

        public async Task<List<Organisation>> GetListAsync(Domaine? domaine, CancellationToken ct)
            => await _repository.GetListAsync(domaine, ct);
    }
}
