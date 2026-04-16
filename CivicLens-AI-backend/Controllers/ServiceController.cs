using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CivicLens.API.Controllers
{
    [Route("api/service")]
    [ApiController]
    [Authorize(Roles = "SERVICE")]
    public class ServiceController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly ISignalementService _signalementService;
        private readonly IHistoriqueService _historiqueService;
        private readonly IOrganisationService _organisationService;
        private readonly INotificationService _notificationService;

        public ServiceController(
            AppDbContext db,
            ISignalementService signalementService,
            IHistoriqueService historiqueService,
            IOrganisationService organisationService,
            INotificationService notificationService)
        {
            _db                  = db;
            _signalementService  = signalementService;
            _historiqueService   = historiqueService;
            _organisationService = organisationService;
            _notificationService = notificationService;
        }

        // ── Résolution de l'acteur ─────────────────────────────────────────

        private Guid? GetCurrentUserId()
        {
            var str = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            return Guid.TryParse(str, out var id) ? id : null;
        }

        private async Task<Service?> GetCurrentServiceAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null) return null;
            return await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == userId.Value, ct);
        }

        // ── GET /api/service/signalements ──────────────────────────────────

        [HttpGet("signalements")]
        public async Task<IActionResult> GetSignalements(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var signalements = await _signalementService.GetListAsync(service.Domaine, service.Delegation, ct);

            return Ok(signalements.Select(s => new
            {
                id               = s.Id,
                titre            = $"Signalement #{s.Id}",
                description      = s.Description,
                descriptionService = s.DescriptionService,
                createdAt        = s.DateCreation,
                statut           = s.Statut.ToString(),
                gravite          = s.Gravite.ToString(),
                priorite         = s.Priorite.ToString(),
                domaine          = s.Domaine.ToString(),
                scoreAI          = s.ScoreAI,
                adresse          = s.Adresse,
                latitude         = s.Latitude,
                longitude        = s.Longitude,
                votes            = s.Votes.Count,
                organisationId   = s.OrganisationId,
                imageUrl         = s.Images.FirstOrDefault()?.UrlImage
            }));
        }

        // ── GET /api/service/signalements/{id} ─────────────────────────────

        [HttpGet("signalements/{id:long}")]
        public async Task<IActionResult> GetSignalementById([FromRoute] long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var signalement = await _signalementService.GetByIdAsync(id, service.Domaine, service.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre domaine/délégation.");

            var historiques = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);

            return Ok(new
            {
                signalement = new
                {
                    id               = signalement.Id,
                    titre            = $"Signalement #{signalement.Id}",
                    description      = signalement.Description,
                    descriptionService = signalement.DescriptionService,
                    createdAt        = signalement.DateCreation,
                    dateResolution   = signalement.DateResolution,
                    statut           = signalement.Statut.ToString(),
                    gravite          = signalement.Gravite.ToString(),
                    priorite         = signalement.Priorite.ToString(),
                    domaine          = signalement.Domaine.ToString(),
                    scoreAI          = signalement.ScoreAI,
                    adresse          = signalement.Adresse,
                    citoyenId        = signalement.CitoyenId,
                    organisationId   = signalement.OrganisationId,
                    votes            = signalement.Votes.Count
                },
                images = signalement.Images.Select(i => i.UrlImage).ToList(),
                historiques
            });
        }

        // ── PUT /api/service/signalements/{id}/ouvrir ──────────────────────

        [HttpPut("signalements/{id:long}/ouvrir")]
        public async Task<IActionResult> OuvrirSignalement([FromRoute] long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var signalement = await _signalementService.GetByIdAsync(id, service.Domaine, service.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre domaine/délégation.");

            if (signalement.Statut != StatutSignalement.EN_ATTENTE)
                return BadRequest($"Impossible d'ouvrir (statut actuel : {signalement.Statut}). Seuls les signalements EN_ATTENTE peuvent être ouverts.");

            signalement.Statut = StatutSignalement.OUVERTE;
            _historiqueService.Add(signalement.Id, StatutSignalement.OUVERTE, service.Id);
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId, signalement.Id,
                "STATUT_OUVERTE",
                $"Votre signalement #{signalement.Id} a été pris en charge et est maintenant à l'état OUVERT.");

            if (!await _historiqueService.SaveAsync(ct))
                return StatusCode(500, "Erreur lors de la sauvegarde.");

            return Ok(new { message = "Signalement ouvert avec succès.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
        }

        // ── PUT /api/service/signalements/{id}/envoyer ─────────────────────

        [HttpPut("signalements/{id:long}/envoyer")]
        public async Task<IActionResult> EnvoyerSignalement(
            [FromRoute] long id, [FromBody] SendSignalementDto dto, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var signalement = await _signalementService.GetByIdAsync(id, service.Domaine, service.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre domaine/délégation.");

            if (signalement.Statut != StatutSignalement.OUVERTE)
                return BadRequest($"Impossible d'envoyer (statut actuel : {signalement.Statut}). Seuls les signalements OUVERTS peuvent être envoyés.");

            var organisation = await _organisationService.GetByIdAsync(dto.OrganisationId, ct);
            if (organisation == null)
                return NotFound("Organisation introuvable.");

            if (signalement.Domaine.HasValue && organisation.DomaineOrganisation != signalement.Domaine.Value)
                return BadRequest($"L'organisation sélectionnée ({organisation.DomaineOrganisation}) ne correspond pas au domaine du signalement ({signalement.Domaine}).");

            signalement.Statut         = StatutSignalement.ENVOYEE;
            signalement.OrganisationId = organisation.Id;
            if (!string.IsNullOrWhiteSpace(dto.DescriptionSupplementaire))
                signalement.DescriptionService = dto.DescriptionSupplementaire.Trim();

            _historiqueService.Add(signalement.Id, StatutSignalement.ENVOYEE, service.Id);
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId, signalement.Id,
                "STATUT_ENVOYEE",
                $"Votre signalement #{signalement.Id} a été transmis à l'organisation compétente ({organisation.NomOrganisation}) pour traitement.");

            if (!await _historiqueService.SaveAsync(ct))
                return StatusCode(500, "Erreur lors de la sauvegarde.");

            return Ok(new
            {
                message         = "Signalement envoyé avec succès à l'organisation.",
                signalementId   = signalement.Id,
                statut          = signalement.Statut.ToString(),
                organisationId  = signalement.OrganisationId,
                organisationNom = organisation.NomOrganisation
            });
        }

        // ── PUT /api/service/signalements/{id}/rejeter ─────────────────────

        [HttpPut("signalements/{id:long}/rejeter")]
        public async Task<IActionResult> RejeterSignalement([FromRoute] long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var signalement = await _signalementService.GetByIdAsync(id, service.Domaine, service.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre domaine/délégation.");

            if (signalement.Statut != StatutSignalement.OUVERTE)
                return BadRequest($"Impossible de rejeter (statut actuel : {signalement.Statut}). Seuls les signalements OUVERTS peuvent être rejetés.");

            signalement.Statut = StatutSignalement.REJETE;
            _historiqueService.Add(signalement.Id, StatutSignalement.REJETE, service.Id);
            _notificationService.PrepareSignalementNotification(
                signalement.CitoyenId, signalement.Id,
                "STATUT_REJETE",
                $"Votre signalement #{signalement.Id} a été examiné mais n'a pas pu être transmis à une organisation. Il a été rejeté.");

            if (!await _historiqueService.SaveAsync(ct))
                return StatusCode(500, "Erreur lors de la sauvegarde.");

            return Ok(new { message = "Signalement rejeté.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
        }

        // ── GET /api/service/stats ─────────────────────────────────────────

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var stats = await _signalementService.GetStatsAsync(service.Domaine, service.Delegation, ct);
            return Ok(stats);
        }

        // ── GET /api/service/organisations ─────────────────────────────────

        [HttpGet("organisations")]
        public async Task<IActionResult> GetOrganisations(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null) return Unauthorized("Service introuvable pour ce token.");

            var organisations = await _organisationService.GetListAsync(service.Domaine, ct);

            return Ok(organisations.Select(o => new
            {
                id                  = o.Id,
                nomOrganisation     = o.NomOrganisation,
                domaineOrganisation = o.DomaineOrganisation.ToString(),
                typeOrganisation    = o.TypeOrganisation.ToString(),
                region              = o.Region,
                delegation          = o.Delegation,
                email               = o.Email
            }));
        }
    }
}
