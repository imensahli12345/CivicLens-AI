using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CivicLens.API.Repository.Classes
{
    public class HistoriqueRepository : IHistoriqueRepository
    {
        private readonly AppDbContext _context;

        public HistoriqueRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<HistoriqueStatut>> GetBySignalementIdAsync(long signalementId, CancellationToken ct)
        {
            return await _context.HistoriquesStatut
                .Where(h => h.SignalementId == signalementId)
                .OrderBy(h => h.DateChangement)
                .ToListAsync(ct);
        }

        public Task<HistoriqueStatut?> GetSingleBySignalementIdAsync(long signalementId, CancellationToken ct)
        {
            return _context.HistoriquesStatut
                .FirstOrDefaultAsync(h => h.SignalementId == signalementId, ct);
        }

        public async Task<List<HistoriqueEntryDto>> GetExpandedBySignalementIdAsync(long signalementId, CancellationToken ct)
        {
            var historiques = await _context.HistoriquesStatut
                .AsNoTracking()
                .Where(h => h.SignalementId == signalementId)
                .OrderBy(h => h.DateChangement)
                .ToListAsync(ct);

            var modifierIds = historiques
                .SelectMany(h => h.IdModificateurs)
                .Distinct()
                .ToList();

            var bureauxById = await _context.BureauxDeControle
                .AsNoTracking()
                .Where(b => modifierIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Matricule, ct);

            var servicesById = await _context.Services
                .AsNoTracking()
                .Where(s => modifierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => s.Nom, ct);

            var result = new List<HistoriqueEntryDto>();
            foreach (var h in historiques)
            {
                var maxCount = Math.Max(h.Statuts.Count, h.IdModificateurs.Count);
                for (var i = 0; i < maxCount; i++)
                {
                    if (i >= h.Statuts.Count)
                        continue;

                    var modifierId = i < h.IdModificateurs.Count ? h.IdModificateurs[i] : (Guid?)null;
                    string? modificateur = null;
                    if (modifierId.HasValue)
                    {
                        if (bureauxById.TryGetValue(modifierId.Value, out var matricule))
                            modificateur = matricule;
                        else if (servicesById.TryGetValue(modifierId.Value, out var serviceNom))
                            modificateur = serviceNom;
                    }

                    result.Add(new HistoriqueEntryDto
                    {
                        Id = h.Id,
                        DateChangement = h.DateChangement,
                        Statut = h.Statuts[i].ToString(),
                        ModificateurId = modifierId,
                        Modificateur = modificateur
                    });
                }
            }

            return result;
        }

        public void Add(HistoriqueStatut historique)
        {
            var existing = _context.HistoriquesStatut
                .FirstOrDefault(h => h.SignalementId == historique.SignalementId);

            if (existing is null)
            {
                _context.HistoriquesStatut.Add(historique);
                return;
            }

            if (historique.Statuts.Count > 0)
                existing.Statuts = [..existing.Statuts, ..historique.Statuts];

            if (historique.IdModificateurs.Count > 0)
                existing.IdModificateurs = [..existing.IdModificateurs, ..historique.IdModificateurs];

            existing.DateChangement = DateTime.UtcNow;

            // EF Core cannot detect changes to value-converted jsonb properties automatically.
            // Explicitly mark them as modified so the serialized JSON is always persisted.
            var entry = _context.Entry(existing);
            entry.Property(h => h.Statuts).IsModified = true;
            entry.Property(h => h.IdModificateurs).IsModified = true;
        }

        public async Task<bool> SaveAsync(CancellationToken ct)
            => await _context.SaveChangesAsync(ct) > 0;
    }
}