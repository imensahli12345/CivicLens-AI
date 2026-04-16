using CivicLens.API.Data;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicLens.API.Repository.Classes
{
    public class OrganisationRepository : IOrganisationRepository
    {
        private readonly AppDbContext _context;

        public OrganisationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Organisation?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Organisations
                .FirstOrDefaultAsync(o => o.Id == id, ct);
        }

        public async Task<List<Organisation>> GetListAsync(Domaine? domaine, CancellationToken ct)
        {
            var q = _context.Organisations
                .Where(o => o.StatutCompte == StatutCompte.ACTIF)
                .AsQueryable();

            if (domaine.HasValue)
                q = q.Where(o => o.DomaineOrganisation == domaine.Value);

            return await q.ToListAsync(ct);
        }
    }
}