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

        public async Task<Organisation?> GetFirstAsync(CancellationToken ct)
        {
            return await _context.Organisations
                .AsNoTracking()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<Organisation>> GetListAsync(Domaine? domaine, CancellationToken ct)
        {
            // Fix any legacy French-accented values in memory by filtering safely
            var all = await _context.Organisations
                .AsNoTracking()
                .ToListAsync(ct);

            var filtered = all.Where(o => o.StatutCompte == StatutCompte.ACTIF);

            if (domaine.HasValue)
                filtered = filtered.Where(o => o.DomaineOrganisation == domaine.Value);

            return filtered.ToList();
        }

        public async Task<string?> GetNameByIdAsync(Guid id, CancellationToken ct)
        {
            return await _context.Organisations
                .AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => o.NomOrganisation)
                .FirstOrDefaultAsync(ct);
        }
    }
}
