using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;

namespace CivicLens.API.Services.Classes
{
    public class SignalementManagementService : ISignalementManagementService
    {
        private readonly ISignalementRepository _signalementRepository;
        private readonly IHistoriqueRepository _historiqueRepository;
        private readonly IOrganisationRepository _organisationRepository;
        private readonly INotificationService _notificationService;

        public SignalementManagementService(
            ISignalementRepository signalementRepository,
            IHistoriqueRepository historiqueRepository,
            IOrganisationRepository organisationRepository,
            INotificationService notificationService)
        {
            _signalementRepository   = signalementRepository;
            _historiqueRepository    = historiqueRepository;
            _organisationRepository  = organisationRepository;
            _notificationService     = notificationService;
        }

        // ── Requêtes ─────────────────────────────────────────────────────────

        public async Task<List<Signalement>> GetSignalementsAsync(Domaine? domaine, string delegation, CancellationToken ct)
            => await _signalementRepository.GetListAsync(domaine, delegation, ct);

        public async Task<Signalement?> GetSignalementByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct)
            => await _signalementRepository.GetByIdAsync(id, domaine, delegation, ct);

        public async Task<List<HistoriqueStatut>> GetHistoriquesAsync(long signalementId, CancellationToken ct)
            => await _historiqueRepository.GetBySignalementIdAsync(signalementId, ct);

        public async Task<Organisation?> GetOrganisationByIdAsync(Guid id, CancellationToken ct)
            => await _organisationRepository.GetByIdAsync(id, ct);

        public async Task<List<Organisation>> GetOrganisationsAsync(Domaine? domaine, CancellationToken ct)
            => await _organisationRepository.GetListAsync(domaine, ct);

        public async Task<SignalementStatsDto> GetStatsAsync(Domaine? domaine, string delegation, CancellationToken ct)
        {
            var counts = await _signalementRepository.CountByStatusAsync(domaine, delegation, ct);
            return new SignalementStatsDto
            {
                Total         = counts.Values.Sum(),
                EnAttente     = counts.GetValueOrDefault(StatutSignalement.EN_ATTENTE),
                Ouvertes      = counts.GetValueOrDefault(StatutSignalement.OUVERTE),
                Envoyees      = counts.GetValueOrDefault(StatutSignalement.ENVOYEE),
                EnCoursDEtude = counts.GetValueOrDefault(StatutSignalement.EN_COURS_DETUDE),
                Resolus       = counts.GetValueOrDefault(StatutSignalement.RESOLU),
                Rejetes       = counts.GetValueOrDefault(StatutSignalement.REJETE),
                Domaine       = domaine?.ToString(),
                Delegation    = delegation
            };
        }

        // ── Transitions de statut ────────────────────────────────────────────

        public async Task<bool> OuvrirSignalementAsync(Signalement signalement, Guid actorId, CancellationToken ct)
        {
            if (signalement.Statut != StatutSignalement.EN_ATTENTE)
                return false;

            signalement.Statut = StatutSignalement.OUVERTE;

            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId   = signalement.Id,
                Statuts         = [StatutSignalement.OUVERTE],
                IdModificateurs = [actorId],
                DateChangement  = DateTime.UtcNow
            });

            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_OUVERTE",
                $"Votre signalement #{signalement.Id} a été pris en charge et est maintenant à l'état OUVERT.");

            return await _historiqueRepository.SaveAsync(ct);
        }

        public async Task<bool> EnvoyerSignalementAsync(Signalement signalement, Guid actorId, Organisation organisation, string? descriptionSupp, CancellationToken ct)
        {
            if (signalement.Statut != StatutSignalement.OUVERTE)
                return false;

            signalement.Statut         = StatutSignalement.ENVOYEE;
            signalement.OrganisationId = organisation.Id;

            if (!string.IsNullOrWhiteSpace(descriptionSupp))
                signalement.DescriptionService = descriptionSupp.Trim();

            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId   = signalement.Id,
                Statuts         = [StatutSignalement.ENVOYEE],
                IdModificateurs = [actorId],
                DateChangement  = DateTime.UtcNow
            });

            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_ENVOYEE",
                $"Votre signalement #{signalement.Id} a été transmis à l'organisation compétente ({organisation.NomOrganisation}) pour traitement.");

            return await _historiqueRepository.SaveAsync(ct);
        }

        public async Task<bool> RejeterSignalementAsync(Signalement signalement, Guid actorId, CancellationToken ct)
        {
            if (signalement.Statut != StatutSignalement.OUVERTE)
                return false;

            signalement.Statut = StatutSignalement.REJETE;

            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId   = signalement.Id,
                Statuts         = [StatutSignalement.REJETE],
                IdModificateurs = [actorId],
                DateChangement  = DateTime.UtcNow
            });

            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_REJETE",
                $"Votre signalement #{signalement.Id} a été examiné mais n'a pas pu être transmis à une organisation. Il a été rejeté.");

            return await _historiqueRepository.SaveAsync(ct);
        }
    }
}
