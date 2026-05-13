using CivicLens.API.Data;
using CivicLens.API.Extensions;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace CivicLens.API.Repository.Classes
{
    public class SignalementManagementRepository : ISignalementRepository
    {
        private readonly AppDbContext _context;

        public SignalementManagementRepository(AppDbContext context)
        {
            _context = context;
        }

        // ── Base query helper ───────────────────────────────────────────────
       private IQueryable<Signalement> BaseQuery(Domaine? domaine, string delegation)
{
    var q = _context.Signalements
        .Include(s => s.Citoyen)
        .Include(s => s.Organisation) // ✅ ADD THIS
        .Include(s => s.Images)
        .Include(s => s.Votes)
        .Where(s => s.Citoyen.Delegation == delegation);

    if (domaine.HasValue)
        q = q.Where(s => s.Domaine == domaine.Value);

    return q;
}
        // ── Queries ─────────────────────────────────────────────────────────

        // Get one signalement by ID
        public async Task<Signalement?> GetByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct)
        {
            return await BaseQuery(domaine, delegation)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        // Get list of signalements (ordered by newest)
        public async Task<List<Signalement>> GetListAsync(Domaine? domaine, string delegation, CancellationToken ct)
        {
            return await BaseQuery(domaine, delegation)
                .OrderByDescending(s => s.DateCreation)
                .ToListAsync(ct);
        }

        public async Task<List<Signalement>> GetFilteredListAsync(
            Domaine? domaine,
            string delegation,
            StatutSignalement? statut,
            Gravite? gravite,
            Priorite? priorite,
            DateTime? dateResolutionFrom,
            DateTime? dateResolutionTo,
            CancellationToken ct)
        {
            return await BaseQuery(domaine, delegation)
                .ApplyFilters(statut, gravite, priorite, dateResolutionFrom, dateResolutionTo)
                .OrderByDescending(s => s.DateCreation)
                .ToListAsync(ct);
        }

        public async Task<Signalement?> GetOrganisationByIdAsync(long id, Guid organisationId, CancellationToken ct)
        {
            return await _context.Signalements
                .Include(s => s.Images)
                .Include(s => s.Votes)
                .FirstOrDefaultAsync(s => s.Id == id && s.OrganisationId == organisationId, ct);
        }

        public async Task<List<Signalement>> GetOrganisationListAsync(
            Guid organisationId,
            StatutSignalement? statut,
            Gravite? gravite,
            Priorite? priorite,
            DateTime? dateResolutionFrom,
            DateTime? dateResolutionTo,
            CancellationToken ct)
        {
            return await _context.Signalements
                .AsNoTracking()
                .Include(s => s.Votes)
                .Include(s => s.Images)
                .Where(s => s.OrganisationId == organisationId)
                .ApplyFilters(statut, gravite, priorite, dateResolutionFrom, dateResolutionTo)
                .OrderByDescending(s => s.DateCreation)
                .ToListAsync(ct);
        }

        // Count signalements by status (stats)
        public async Task<Dictionary<StatutSignalement, int>> CountByStatusAsync(
            Domaine? domaine, string delegation, CancellationToken ct)
        {
            var q = _context.Signalements
                .Where(s => s.Citoyen.Delegation == delegation)
                .AsQueryable();

            if (domaine.HasValue)
                q = q.Where(s => s.Domaine == domaine.Value);

            return await q
                .GroupBy(s => s.Statut)
                .Select(g => new { Statut = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Statut, x => x.Count, ct);
        }

        public async Task<string?> GetCinByCitoyenIdAsync(Guid citoyenId, CancellationToken ct)
        {
            return await _context.Citoyens
                .AsNoTracking()
                .Where(c => c.Id == citoyenId)
                .Select(c => c.Cin)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<bool> SaveAsync(CancellationToken ct)
        {
            return await _context.SaveChangesAsync(ct) > 0;
        }
    }
}
