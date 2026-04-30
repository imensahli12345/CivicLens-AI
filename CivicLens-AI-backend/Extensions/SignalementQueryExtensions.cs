using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Extensions
{
    public static class SignalementQueryExtensions
    {
        public static IQueryable<Signalement> ApplyFilters(
            this IQueryable<Signalement> query,
            StatutSignalement? statut,
            Gravite? gravite,
            Priorite? priorite,
            DateTime? dateResolutionFrom,
            DateTime? dateResolutionTo)
        {
            if (statut.HasValue)
                query = query.Where(s => s.Statut == statut.Value);

            if (gravite.HasValue)
                query = query.Where(s => s.Gravite == gravite.Value);

            if (priorite.HasValue)
                query = query.Where(s => s.Priorite == priorite.Value);

            if (dateResolutionFrom.HasValue)
            {
                var from = dateResolutionFrom.Value.Date;
                query = query.Where(s => s.DateResolution.HasValue && s.DateResolution.Value >= from);
            }

            if (dateResolutionTo.HasValue)
            {
                var toExclusive = dateResolutionTo.Value.Date.AddDays(1);
                query = query.Where(s => s.DateResolution.HasValue && s.DateResolution.Value < toExclusive);
            }

            return query;
        }
    }
}
