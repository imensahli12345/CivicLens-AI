using CivicLens.API.Data;
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
                .Include(s => s.Votes)
                .Include(s => s.Images)
                .Where(s => s.Citoyen.Delegation == delegation)
                .AsQueryable();

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
    }
}