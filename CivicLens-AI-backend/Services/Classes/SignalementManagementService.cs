using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Data;
using CivicLens.API.Repository.Interfaces;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CivicLens.API.Services.Classes
{
    public class SignalementManagementService : ISignalementManagementService
    {
        private readonly ISignalementRepository _signalementRepository;
        private readonly IHistoriqueRepository _historiqueRepository;
        private readonly IOrganisationRepository _organisationRepository;
        private readonly INotificationService _notificationService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly AppDbContext _db;
        private readonly ILogger<SignalementManagementService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly string _uploadsRoot;

        public SignalementManagementService(
            ISignalementRepository signalementRepository,
            IHistoriqueRepository historiqueRepository,
            IOrganisationRepository organisationRepository,
            INotificationService notificationService,
            IWhatsAppService whatsAppService,
            AppDbContext db,
            ILogger<SignalementManagementService> logger,
            IWebHostEnvironment env,
            IConfiguration config)
        {
            _signalementRepository = signalementRepository;
            _historiqueRepository = historiqueRepository;
            _organisationRepository = organisationRepository;
            _notificationService = notificationService;
            _whatsAppService = whatsAppService;
            _db = db;
            _logger = logger;
            _env = env;
            _uploadsRoot = config["UploadsRoot"] ?? Path.Combine(_env.ContentRootPath, "uploads");
        }

        private Task<string?> GetCitoyenTelephoneAsync(Guid citoyenId, CancellationToken ct)
        {
            return _db.Citoyens
                .AsNoTracking()
                .Where(c => c.Id == citoyenId)
                .Select(c => c.Telephone)
                .FirstOrDefaultAsync(ct);
        }

        private async Task TrySendWhatsAppAsync(Guid citoyenId, string message, CancellationToken ct)
        {
            var tel = await GetCitoyenTelephoneAsync(citoyenId, ct);
            if (string.IsNullOrWhiteSpace(tel))
            {
                _logger.LogInformation("WhatsApp skipped: citoyen {CitoyenId} has no phone number.", citoyenId);
                return;
            }

            var telDigits = new string(tel.Where(char.IsDigit).ToArray());
            var telSuffix = telDigits.Length >= 4 ? telDigits[^4..] : telDigits;
            _logger.LogInformation("WhatsApp: attempting send to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);

            var ok = await _whatsAppService.SendTextAsync(tel, message, ct);
            if (ok)
            {
                _logger.LogInformation("WhatsApp: text sent to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);
                return;
            }
            else
            {
                _logger.LogWarning("WhatsApp text failed for citoyen {CitoyenId} (signalement update).", citoyenId);
                var fallbackOk = await _whatsAppService.SendHelloWorldAsync(tel, ct);
                if (!fallbackOk)
                    _logger.LogWarning("WhatsApp fallback hello_world also failed for citoyen {CitoyenId}.", citoyenId);
                else
                    _logger.LogInformation("WhatsApp: fallback hello_world sent to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);
            }
        }

        private static (StatutSignalement? statut, Gravite? gravite, Priorite? priorite) ParseFilter(SignalementFilterDto filter)
        {
            if (!filter.TryParse(out var statut, out var gravite, out var priorite, out var error))
                throw new ArgumentException(error);

            return (statut, gravite, priorite);
        }

        private static void EnsureValidActorId(Guid actorId)
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("actorId est obligatoire et ne peut pas etre Guid.Empty.", nameof(actorId));
        }

        private async Task<SignalementDto> MapToDtoAsync(Signalement s, CancellationToken ct)
        {
            var cin = s.CitoyenId != Guid.Empty ? await _signalementRepository.GetCinByCitoyenIdAsync(s.CitoyenId, ct) : null;
            var orgNom = s.OrganisationId.HasValue ? (await _organisationRepository.GetByIdAsync(s.OrganisationId.Value, ct))?.NomOrganisation : null;
            
            return new SignalementDto
            {
                Id = s.Id,
                Titre = $"Signalement #{s.Id}",
                Description = s.Description,
                DescriptionService = s.DescriptionService,
                RapportUrl = s.RapportPath,
                DateCreation = s.DateCreation,
                DateResolution = s.DateResolution,
                Statut = s.Statut.ToString(),
                Gravite = s.Gravite.ToString(),
                Priorite = s.Priorite.ToString(),
                Domaine = s.Domaine?.ToString(),
                Adresse = s.Adresse,
                CinCitoyen = cin,
                OrganisationNom = orgNom,
                Images = s.Images?.Select(i => i.UrlImage).ToList() ?? new List<string>()
            };
        }

        public async Task<List<SignalementDto>> GetSignalementsAsync(Domaine? domaine, string delegation, SignalementFilterDto filter, CancellationToken ct)
        {
            var parsed = ParseFilter(filter);
            var items = await _signalementRepository.GetFilteredListAsync(
                domaine,
                delegation,
                parsed.statut,
                parsed.gravite,
                parsed.priorite,
                filter.DateResolutionFrom,
                filter.DateResolutionTo,
                ct);

            var result = new List<SignalementDto>();
            foreach (var item in items)
            {
                result.Add(await MapToDtoAsync(item, ct));
            }
            return result;
        }

        public async Task<SignalementDto?> GetSignalementByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct)
        {
            var s = await _signalementRepository.GetByIdAsync(id, domaine, delegation, ct);
            if (s == null) return null;
            return await MapToDtoAsync(s, ct);
        }

        public async Task<List<SignalementDto>> GetOrganisationSignalementsAsync(Guid organisationId, SignalementFilterDto filter, CancellationToken ct)
        {
            var parsed = ParseFilter(filter);
            var items = await _signalementRepository.GetOrganisationListAsync(
                organisationId,
                parsed.statut,
                parsed.gravite,
                parsed.priorite,
                filter.DateResolutionFrom,
                filter.DateResolutionTo,
                ct);

            var result = new List<SignalementDto>();
            foreach (var item in items)
            {
                result.Add(await MapToDtoAsync(item, ct));
            }
            return result;
        }
//this method following the arhcitecture of Application project sagemcom

        public async Task<List<SignalementDto>> GetAllSignalementsAsync(Domaine? domaine, string delegation, CancellationToken ct)
        {
            var signalements = await _signalementRepository.GetListAsync(domaine, delegation, ct);
            var result = new List<SignalementDto>();
            foreach (var s in signalements)
            {
                result.Add(await MapToDtoAsync(s, ct));
            }
            return result;
        }

        public async Task<List<SignalementDto>> GetAllOrganisationSignalementsAsync(Guid organisationId, CancellationToken ct)
        {
            var items = await _signalementRepository.GetOrganisationListAsync(organisationId, null, null, null, null, null, ct);
            var result = new List<SignalementDto>();
            foreach (var item in items)
            {
                result.Add(await MapToDtoAsync(item, ct));
            }
            return result;
        }

        public async Task<string?> GetCinByCitoyenIdAsync(Guid citoyenId, CancellationToken ct)
            => await _signalementRepository.GetCinByCitoyenIdAsync(citoyenId, ct);

        public async Task<SignalementDto?> GetOrganisationSignalementByIdAsync(long signalementId, Guid organisationId, CancellationToken ct)
        {
            var s = await _signalementRepository.GetOrganisationByIdAsync(signalementId, organisationId, ct);
            if (s == null) return null;
            return await MapToDtoAsync(s, ct);
        }

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
                Total = counts.Values.Sum(),
                EnAttente = counts.GetValueOrDefault(StatutSignalement.EN_ATTENTE),
                Ouvertes = counts.GetValueOrDefault(StatutSignalement.OUVERTE),
                Envoyees = counts.GetValueOrDefault(StatutSignalement.ENVOYEE),
                EnCoursDEtude = counts.GetValueOrDefault(StatutSignalement.EN_COURS_DETUDE),
                Resolus = counts.GetValueOrDefault(StatutSignalement.RESOLU),
                Rejetes = counts.GetValueOrDefault(StatutSignalement.REJETE),
                Domaine = domaine?.ToString(),
                Delegation = delegation
            };
        }

        public async Task<Signalement> OuvrirSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, CancellationToken ct)
        {
            EnsureValidActorId(actorId);

            var signalement = await _signalementRepository.GetByIdAsync(signalementId, domaine, delegation, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour ce contexte.");

            if (signalement.Statut != StatutSignalement.EN_ATTENTE)
                throw new InvalidOperationException($"Impossible d'ouvrir (statut actuel : {signalement.Statut}).");

            signalement.Statut = StatutSignalement.OUVERTE;
            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId = signalement.Id,
                Statuts = [StatutSignalement.OUVERTE],
                IdModificateurs = [actorId],
                DateChangement = DateTime.UtcNow
            });

            var message = $"Statut mis a jour : votre signalement #{signalement.Id} est maintenant OUVERT.";
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_OUVERTE",
                message);
            await TrySendWhatsAppAsync(signalement.CitoyenId, message, ct);

            await _historiqueRepository.SaveAsync(ct);
            return signalement;
        }

        public async Task<Signalement> EnvoyerSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, Guid organisationId, string? descriptionSupp, CancellationToken ct)
        {
            EnsureValidActorId(actorId);

            var signalement = await _signalementRepository.GetByIdAsync(signalementId, domaine, delegation, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour ce contexte.");
            var organisation = await _organisationRepository.GetByIdAsync(organisationId, ct)
                ?? throw new KeyNotFoundException("Organisation introuvable.");

            if (signalement.Statut != StatutSignalement.OUVERTE)
                throw new InvalidOperationException($"Impossible d'envoyer (statut actuel : {signalement.Statut}).");

            if (signalement.Domaine.HasValue && organisation.DomaineOrganisation != signalement.Domaine.Value)
                throw new InvalidOperationException($"L'organisation selectionnee ({organisation.DomaineOrganisation}) ne correspond pas au domaine du signalement ({signalement.Domaine}).");

            signalement.Statut = StatutSignalement.ENVOYEE;
            signalement.OrganisationId = organisation.Id;
            if (!string.IsNullOrWhiteSpace(descriptionSupp))
                signalement.DescriptionService = descriptionSupp.Trim();

            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId = signalement.Id,
                Statuts = [StatutSignalement.ENVOYEE],
                IdModificateurs = [actorId],
                DateChangement = DateTime.UtcNow
            });

            var message = $"Signalement #{signalement.Id} transmis a {organisation.NomOrganisation}. Statut : ENVOYEE.";
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_ENVOYEE",
                message);
            await TrySendWhatsAppAsync(signalement.CitoyenId, message, ct);

            await _historiqueRepository.SaveAsync(ct);
            return signalement;
        }

        public async Task<Signalement> RejeterSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, CancellationToken ct)
        {
            EnsureValidActorId(actorId);

            var signalement = await _signalementRepository.GetByIdAsync(signalementId, domaine, delegation, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour ce contexte.");

            if (signalement.Statut != StatutSignalement.OUVERTE)
                throw new InvalidOperationException($"Impossible de rejeter (statut actuel : {signalement.Statut}).");

            signalement.Statut = StatutSignalement.REJETE;
            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId = signalement.Id,
                Statuts = [StatutSignalement.REJETE],
                IdModificateurs = [actorId],
                DateChangement = DateTime.UtcNow
            });

            var message = $"Signalement #{signalement.Id} rejete apres verification.";
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_REJETE",
                message);
            await TrySendWhatsAppAsync(signalement.CitoyenId, message, ct);

            await _historiqueRepository.SaveAsync(ct);
            return signalement;
        }

        public async Task<Signalement> CommencerEtudeOrganisationAsync(long signalementId, Guid organisationId, Guid actorId, CancellationToken ct)
        {
            EnsureValidActorId(actorId);

            var signalement = await _signalementRepository.GetOrganisationByIdAsync(signalementId, organisationId, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour cette organisation.");

            if (signalement.Statut != StatutSignalement.ENVOYEE)
                throw new InvalidOperationException($"Impossible d'ouvrir (statut actuel : {signalement.Statut}).");

            signalement.Statut = StatutSignalement.EN_COURS_DETUDE;
            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId = signalement.Id,
                Statuts = [StatutSignalement.EN_COURS_DETUDE],
                IdModificateurs = [actorId],
                DateChangement = DateTime.UtcNow
            });

            var message = $"Signalement #{signalement.Id} : en cours d'etude par l'organisation.";
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_EN_COURS_DETUDE",
                message);
            await TrySendWhatsAppAsync(signalement.CitoyenId, message, ct);

            await _historiqueRepository.SaveAsync(ct);
            return signalement;
        }

        public async Task<Signalement> ResoudreSignalementOrganisationAsync(long signalementId, Guid organisationId, Guid actorId, CancellationToken ct)
        {
            EnsureValidActorId(actorId);

            var signalement = await _signalementRepository.GetOrganisationByIdAsync(signalementId, organisationId, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour cette organisation.");

            if (signalement.Statut != StatutSignalement.ENVOYEE && signalement.Statut != StatutSignalement.EN_COURS_DETUDE)
                throw new InvalidOperationException($"Impossible de resoudre (statut actuel : {signalement.Statut}).");

            if (string.IsNullOrWhiteSpace(signalement.RapportPath))
                throw new InvalidOperationException("Impossible de resoudre le signalement sans avoir attache un rapport PDF.");

            signalement.Statut = StatutSignalement.RESOLU;
            signalement.DateResolution = DateTime.UtcNow;
            _historiqueRepository.Add(new HistoriqueStatut
            {
                SignalementId = signalement.Id,
                Statuts = [StatutSignalement.RESOLU],
                IdModificateurs = [actorId],
                DateChangement = DateTime.UtcNow
            });

            var message = $"Signalement #{signalement.Id} : resolu par l'organisation.";
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId,
                signalement.Id,
                "STATUT_RESOLU",
                message);
            await TrySendWhatsAppAsync(signalement.CitoyenId, message, ct);

            await _historiqueRepository.SaveAsync(ct);
            return signalement;
        }

        public async Task<string> UploadRapportAsync(long signalementId, Guid organisationId, IFormFile rapport, CancellationToken ct)
        {
            if (rapport == null || rapport.Length == 0)
                throw new ArgumentException("Le rapport PDF est requis.");

            if (!string.Equals(rapport.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Le rapport doit etre un fichier PDF.");

            var extension = Path.GetExtension(rapport.FileName);
            if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Le rapport doit avoir l'extension .pdf.");

            var signalement = await _signalementRepository.GetOrganisationByIdAsync(signalementId, organisationId, ct)
                ?? throw new KeyNotFoundException("Signalement introuvable pour cette organisation.");

            var folder = Path.Combine(_uploadsRoot, "rapports", "signalements", signalement.Id.ToString());
            Directory.CreateDirectory(folder);

            var fileName = $"{Guid.NewGuid():N}.pdf";
            var physicalPath = Path.Combine(folder, fileName);
            var relativePath = $"/uploads/rapports/signalements/{signalement.Id}/{fileName}";
            var previousPath = signalement.RapportPath;

            try
            {
                await using (var stream = System.IO.File.Create(physicalPath))
                {
                    await rapport.CopyToAsync(stream, ct);
                }

                signalement.RapportPath = relativePath;
                await _signalementRepository.SaveAsync(ct);

                if (!string.IsNullOrWhiteSpace(previousPath))
                {
                    var previousPhysicalPath = Path.Combine(
                        _uploadsRoot,
                        previousPath.TrimStart('/').Replace("uploads/", "", StringComparison.OrdinalIgnoreCase)
                            .TrimStart('/')
                            .Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(previousPhysicalPath))
                        System.IO.File.Delete(previousPhysicalPath);
                }

                return relativePath;
            }
            catch
            {
                if (System.IO.File.Exists(physicalPath))
                    System.IO.File.Delete(physicalPath);

                throw;
            }
        }
    }
}
