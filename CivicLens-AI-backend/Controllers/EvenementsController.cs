using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace CivicLens.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "CITOYEN")]
    public class EvenementsController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWhatsAppService _whatsApp;
        private readonly IConfiguration _config;
        private readonly ILogger<EvenementsController> _logger;

        public EvenementsController(
            AppDbContext db,
            IWhatsAppService whatsApp,
            IConfiguration config,
            ILogger<EvenementsController> logger)
        {
            _db = db;
            _whatsApp = whatsApp;
            _config = config;
            _logger = logger;
        }

        private bool WhatsAppEnabled()
        {
            var enabledStr = _config["WhatsApp:Enabled"];
            return bool.TryParse(enabledStr, out var enabled) && enabled;
        }

        private int GetMaxRecipientsPerBatch()
        {
            var maxStr = _config["WhatsApp:MaxRecipientsPerBatch"];
            return int.TryParse(maxStr, out var max) && max > 0 ? max : 5;
        }

        private async Task TrySendWhatsAppTextAsync(Guid citoyenId, string? tel, string message, CancellationToken ct)
        {
            if (!WhatsAppEnabled())
                return;

            if (string.IsNullOrWhiteSpace(tel))
            {
                _logger.LogInformation("WhatsApp skipped: citoyen {CitoyenId} has no phone number.", citoyenId);
                return;
            }

            var telDigits = new string(tel.Where(char.IsDigit).ToArray());
            var telSuffix = telDigits.Length >= 4 ? telDigits[^4..] : telDigits;

            _logger.LogInformation("WhatsApp: attempting send to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);
            var ok = await _whatsApp.SendTextAsync(tel, message, ct);
            if (ok)
            {
                _logger.LogInformation("WhatsApp: text sent to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);
                return;
            }

            _logger.LogWarning("WhatsApp text failed for citoyen {CitoyenId} (evenement).", citoyenId);
            var fallbackOk = await _whatsApp.SendHelloWorldAsync(tel, ct);
            if (!fallbackOk)
                _logger.LogWarning("WhatsApp fallback hello_world also failed for citoyen {CitoyenId}.", citoyenId);
            else
                _logger.LogInformation("WhatsApp: fallback hello_world sent to citoyen {CitoyenId} (..{Suffix}).", citoyenId, telSuffix);
        }

        [HttpPost]
        public async Task<IActionResult> CreateEvenement([FromBody] CreateEvenementDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FirstOrDefaultAsync(c => c.Id == citoyenId, ct);
            if (citoyen == null)
                return Unauthorized("Citoyen introuvable.");

            var evenement = new Evenement
            {
                Titre = dto.Titre.Trim(),
                Description = dto.Description.Trim(),
                DateEvenement = dto.DateEvenement,
                Lieu = dto.Lieu.Trim(),
                Delegation = citoyen.Delegation ?? string.Empty,
                NombreVolontairesRequis = dto.NombreVolontairesRequis,
                OrganisateurId = citoyenId
            };

            _db.Evenements.Add(evenement);
            await _db.SaveChangesAsync(ct);

            var delegation = (citoyen.Delegation ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(delegation))
            {
                var recipients = await _db.Citoyens
                    .AsNoTracking()
                    .Where(c => c.Id != citoyenId && c.Delegation != null && EF.Functions.ILike(c.Delegation, delegation))
                    .Select(c => new { c.Id, c.Telephone })
                    .ToListAsync(ct);

                foreach (var r in recipients)
                {
                    _db.Notifications.Add(new Notification
                    {
                        CitoyenId = r.Id,
                        Type = "EVENEMENT_NOUVEAU_DELEGATION",
                        RelatedEvenementId = evenement.Id,
                        Message = $"Nouveau événement dans votre délégation : « {evenement.Titre} » le {evenement.DateEvenement:dd/MM/yyyy HH:mm} à {evenement.Lieu}.",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _db.SaveChangesAsync(ct);

                var message = $"Nouveau événement dans votre délégation : {evenement.Titre} ({evenement.DateEvenement:dd/MM HH:mm}) - {evenement.Lieu}.";
                foreach (var r in recipients.Where(x => !string.IsNullOrWhiteSpace(x.Telephone)).Take(GetMaxRecipientsPerBatch()))
                {
                    await TrySendWhatsAppTextAsync(r.Id, r.Telephone, message, ct);
                }
            }

            return CreatedAtAction(nameof(GetById), new { id = evenement.Id }, new { id = evenement.Id, message = "Événement créé avec succès." });
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var evenements = await _db.Evenements
                .Include(e => e.Organisateur)
                .Include(e => e.Participations)
                .OrderByDescending(e => e.DateEvenement)
                .AsNoTracking()
                .Select(e => new EvenementDto
                {
                    Id = e.Id,
                    Titre = e.Titre,
                    Description = e.Description,
                    DateEvenement = e.DateEvenement,
                    Lieu = e.Lieu,
                    Delegation = e.Delegation,
                    NombreVolontairesRequis = e.NombreVolontairesRequis,
                    OrganisateurId = e.OrganisateurId,
                    OrganisateurNom = e.Organisateur.Nom + " " + e.Organisateur.Prenom,
                    NombreParticipations = e.Participations.Count,
                    IsConfirmed = e.IsConfirmed,
                    ConfirmedAt = e.ConfirmedAt,
                    Participations = e.Participations.Select(p => new ParticipationDto
                    {
                        Id = p.Id,
                        DateParticipation = p.DateParticipation,
                        Commentaire = p.Commentaire,
                        PresenceConfirmee = p.PresenceConfirmee,
                        CitoyenId = p.CitoyenId,
                        EvenementId = p.EvenementId
                    }).ToList()
                })
                .ToListAsync(ct);

            return Ok(evenements);
        }

        [HttpGet("mes")]
        public async Task<IActionResult> GetMyEvenements(CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var evenements = await _db.Evenements
                .Include(e => e.Organisateur)
                .Include(e => e.Participations)
                .Where(e => e.OrganisateurId == citoyenId)
                .OrderByDescending(e => e.DateEvenement)
                .AsNoTracking()
                .Select(e => new EvenementDto
                {
                    Id = e.Id,
                    Titre = e.Titre,
                    Description = e.Description,
                    DateEvenement = e.DateEvenement,
                    Lieu = e.Lieu,
                    Delegation = e.Delegation,
                    NombreVolontairesRequis = e.NombreVolontairesRequis,
                    OrganisateurId = e.OrganisateurId,
                    OrganisateurNom = e.Organisateur.Nom + " " + e.Organisateur.Prenom,
                    NombreParticipations = e.Participations.Count,
                    IsConfirmed = e.IsConfirmed,
                    ConfirmedAt = e.ConfirmedAt,
                    Participations = e.Participations.Select(p => new ParticipationDto
                    {
                        Id = p.Id,
                        DateParticipation = p.DateParticipation,
                        Commentaire = p.Commentaire,
                        PresenceConfirmee = p.PresenceConfirmee,
                        CitoyenId = p.CitoyenId,
                        EvenementId = p.EvenementId
                    }).ToList()
                })
                .ToListAsync(ct);

            return Ok(evenements);
        }

        [HttpGet("region")]
        public async Task<IActionResult> GetRegionEvenements(CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FirstOrDefaultAsync(c => c.Id == citoyenId, ct);
            if (citoyen == null)
                return Unauthorized("Citoyen introuvable.");

            var delegation = citoyen.Delegation;
            if (string.IsNullOrWhiteSpace(delegation))
                return Ok(new List<EvenementDto>()); // Pas de délégation, pas d'événements régionaux

            var evenements = await _db.Evenements
                .Include(e => e.Organisateur)
                .Include(e => e.Participations)
                .Where(e => EF.Functions.ILike(e.Delegation, $"%{delegation.Trim()}%") && e.OrganisateurId != citoyenId)
                .OrderByDescending(e => e.DateEvenement)
                .AsNoTracking()
                .Select(e => new EvenementDto
                {
                    Id = e.Id,
                    Titre = e.Titre,
                    Description = e.Description,
                    DateEvenement = e.DateEvenement,
                    Lieu = e.Lieu,
                    Delegation = e.Delegation,
                    NombreVolontairesRequis = e.NombreVolontairesRequis,
                    OrganisateurId = e.OrganisateurId,
                    OrganisateurNom = e.Organisateur.Nom + " " + e.Organisateur.Prenom,
                    NombreParticipations = e.Participations.Count,
                    IsConfirmed = e.IsConfirmed,
                    ConfirmedAt = e.ConfirmedAt,
                    Participations = e.Participations.Select(p => new ParticipationDto
                    {
                        Id = p.Id,
                        DateParticipation = p.DateParticipation,
                        Commentaire = p.Commentaire,
                        PresenceConfirmee = p.PresenceConfirmee,
                        CitoyenId = p.CitoyenId,
                        EvenementId = p.EvenementId
                    }).ToList()
                })
                .ToListAsync(ct);

            return Ok(evenements);
        }

        [HttpGet("{id:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct)
        {
            var evenement = await _db.Evenements
                .Include(e => e.Organisateur)
                .Include(e => e.Participations)
                .ThenInclude(p => p.Citoyen)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (evenement == null)
                return NotFound("Événement introuvable.");

            var dto = new EvenementDto
            {
                Id = evenement.Id,
                Titre = evenement.Titre,
                Description = evenement.Description,
                DateEvenement = evenement.DateEvenement,
                Lieu = evenement.Lieu,
                Delegation = evenement.Delegation,
                NombreVolontairesRequis = evenement.NombreVolontairesRequis,
                OrganisateurId = evenement.OrganisateurId,
                OrganisateurNom = evenement.Organisateur.Nom + " " + evenement.Organisateur.Prenom,
                NombreParticipations = evenement.Participations.Count,
                    IsConfirmed = evenement.IsConfirmed,
                    ConfirmedAt = evenement.ConfirmedAt,
                Participations = evenement.Participations.Select(p => new ParticipationDto
                {
                    Id = p.Id,
                    DateParticipation = p.DateParticipation,
                    Commentaire = p.Commentaire,
                    PresenceConfirmee = p.PresenceConfirmee,
                    CitoyenId = p.CitoyenId,
                    CitoyenNom = p.Citoyen.Nom + " " + p.Citoyen.Prenom,
                    EvenementId = p.EvenementId
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost("{id:guid}/participer")]
        public async Task<IActionResult> Participate([FromRoute] Guid id, [FromBody] CreateParticipationDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var evenement = await _db.Evenements.FindAsync(new object[] { id }, ct);
            if (evenement == null)
                return NotFound("Événement introuvable.");

            if (evenement.OrganisateurId == citoyenId)
                return BadRequest("Vous ne pouvez pas participer à un événement que vous avez créé.");

            var existingParticipation = await _db.Participations
                .FirstOrDefaultAsync(p => p.EvenementId == id && p.CitoyenId == citoyenId, ct);

            if (existingParticipation != null)
                return BadRequest("Vous participez déjà à cet événement.");

            var participation = new Participation
            {
                EvenementId = id,
                CitoyenId = citoyenId,
                Commentaire = dto.Commentaire?.Trim() ?? string.Empty,
                DateParticipation = DateTime.UtcNow,
                PresenceConfirmee = false
            };

            _db.Participations.Add(participation);
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Participation enregistrée avec succès." });
        }

        [HttpDelete("{id:guid}/participer")]
        public async Task<IActionResult> CancelParticipation([FromRoute] Guid id, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var participation = await _db.Participations
                .Include(p => p.Evenement)
                .FirstOrDefaultAsync(p => p.EvenementId == id && p.CitoyenId == citoyenId, ct);

            if (participation == null)
                return NotFound("Participation introuvable.");

            var timeUntilEvent = participation.Evenement.DateEvenement - DateTime.UtcNow;
            if (timeUntilEvent.TotalDays < 3)
            {
                return BadRequest("Vous ne pouvez plus annuler votre participation à moins de 3 jours de l'événement.");
            }

            _db.Participations.Remove(participation);
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Participation annulée avec succès." });
        }

        [HttpPut("{id:guid}/participation/commentaire")]
        public async Task<IActionResult> UpdateParticipationCommentaire([FromRoute] Guid id, [FromBody] UpdateParticipationCommentaireDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var participation = await _db.Participations
                .Include(p => p.Evenement)
                .FirstOrDefaultAsync(p => p.EvenementId == id && p.CitoyenId == citoyenId, ct);

            if (participation == null)
                return NotFound("Participation introuvable.");

            var timeUntilEvent = participation.Evenement.DateEvenement - DateTime.UtcNow;
            if (timeUntilEvent.TotalHours < 24)
                return BadRequest("La modification du commentaire est impossible à moins de 24 heures de l'événement.");

            participation.Commentaire = (dto.Commentaire ?? string.Empty).Trim();
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Commentaire mis à jour.", commentaire = participation.Commentaire });
        }

        // ─────────────────────────────────────────────────────────────
        // PUT /api/evenements/{id}
        // Modification (lieu + date) — Organisateur uniquement
        // Interdit si l'événement démarre dans moins de 24h
        // ─────────────────────────────────────────────────────────────
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> UpdateEvenement([FromRoute] Guid id, [FromBody] UpdateEvenementDto dto, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var evenement = await _db.Evenements.FirstOrDefaultAsync(e => e.Id == id, ct);
            if (evenement == null)
                return NotFound("Événement introuvable.");

            if (evenement.OrganisateurId != citoyenId)
                return Forbid("Seul l'organisateur peut modifier cet événement.");

            var timeUntilEvent = evenement.DateEvenement - DateTime.UtcNow;
            if (timeUntilEvent.TotalHours < 24)
                return BadRequest("La modification est impossible à moins de 24 heures de l'événement.");

            if (string.IsNullOrWhiteSpace(dto.Lieu))
                return BadRequest("Le lieu est requis.");

            var newDate = dto.DateEvenement;
            if (newDate <= DateTime.UtcNow)
                return BadRequest("La date de l'événement doit être dans le futur.");

            evenement.Lieu = dto.Lieu.Trim();
            evenement.DateEvenement = newDate;

            // Notifications aux citoyens ayant déjà participé
            var participantIds = await _db.Participations
                .AsNoTracking()
                .Where(p => p.EvenementId == id && p.CitoyenId != citoyenId)
                .Select(p => p.CitoyenId)
                .Distinct()
                .ToListAsync(ct);

            var participantPhones = await _db.Citoyens
                .AsNoTracking()
                .Where(c => participantIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Telephone })
                .ToListAsync(ct);

            foreach (var participantId in participantIds)
            {
                _db.Notifications.Add(new Notification
                {
                    CitoyenId = participantId,
                    Type = "EVENEMENT_MODIFIE",
                    RelatedEvenementId = id,
                    Message =
                        $"L'événement « {evenement.Titre} » a été modifié. Nouvelle date : {evenement.DateEvenement:dd/MM/yyyy HH:mm} - nouveau lieu : {evenement.Lieu}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var msg = $"L'événement « {evenement.Titre} » a été modifié. Nouvelle date : {evenement.DateEvenement:dd/MM/yyyy HH:mm} - nouveau lieu : {evenement.Lieu}.";
            foreach (var r in participantPhones.Where(x => !string.IsNullOrWhiteSpace(x.Telephone)).Take(GetMaxRecipientsPerBatch()))
            {
                await TrySendWhatsAppTextAsync(r.Id, r.Telephone, msg, ct);
            }

            return Ok(new { message = "Événement modifié avec succès." });
        }

        // ─────────────────────────────────────────────────────────────
        // DELETE /api/evenements/{id}
        // Suppression — Organisateur uniquement
        // Interdit si l'événement démarre dans moins de 24h
        // ─────────────────────────────────────────────────────────────
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteEvenement([FromRoute] Guid id, CancellationToken ct)
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            var evenement = await _db.Evenements
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (evenement == null)
                return NotFound("Événement introuvable.");

            if (evenement.OrganisateurId != citoyenId)
                return Forbid("Seul l'organisateur peut supprimer cet événement.");

            var timeUntilEvent = evenement.DateEvenement - DateTime.UtcNow;
            if (timeUntilEvent.TotalHours < 24)
                return BadRequest("La suppression est impossible à moins de 24 heures de l'événement.");

            // Notifications avant suppression
            var participantIds = await _db.Participations
                .AsNoTracking()
                .Where(p => p.EvenementId == id && p.CitoyenId != citoyenId)
                .Select(p => p.CitoyenId)
                .Distinct()
                .ToListAsync(ct);

            var participantPhones = await _db.Citoyens
                .AsNoTracking()
                .Where(c => participantIds.Contains(c.Id))
                .Select(c => new { c.Id, c.Telephone })
                .ToListAsync(ct);

            foreach (var participantId in participantIds)
            {
                _db.Notifications.Add(new Notification
                {
                    CitoyenId = participantId,
                    Type = "EVENEMENT_SUPPRIME",
                    RelatedEvenementId = id,
                    Message = $"L'événement « {evenement.Titre} » a été supprimé par l'organisateur.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (evenement.Participations.Any())
            {
                _db.Participations.RemoveRange(evenement.Participations);
            }

            _db.Evenements.Remove(evenement);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            var msg = $"L'événement « {evenement.Titre} » a été annulé par l'organisateur.";
            foreach (var r in participantPhones.Where(x => !string.IsNullOrWhiteSpace(x.Telephone)).Take(GetMaxRecipientsPerBatch()))
            {
                await TrySendWhatsAppTextAsync(r.Id, r.Telephone, msg, ct);
            }

            return Ok(new { message = "Événement supprimé avec succès." });
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/evenements/{id}/confirm
        // Confirmation — Organisateur uniquement
        // Possible uniquement entre 20h et 10h avant l'événement
        // ─────────────────────────────────────────────────────────────
        [HttpPost("{id:guid}/confirm")]
        public async Task<IActionResult> ConfirmEvenement([FromRoute] Guid id, CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var citoyenId))
                return Unauthorized("Token invalide.");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var evenement = await _db.Evenements
                .Include(e => e.Participations)
                .FirstOrDefaultAsync(e => e.Id == id, ct);

            if (evenement == null)
                return NotFound("Événement introuvable.");

            if (evenement.OrganisateurId != citoyenId)
                return Forbid("Seul l'organisateur peut confirmer cet événement.");

            if (evenement.IsConfirmed)
                return BadRequest("Cet événement est déjà confirmé.");

            var hoursUntilEvent = (evenement.DateEvenement - DateTime.UtcNow).TotalHours;
            if (hoursUntilEvent > 20)
                return BadRequest("La confirmation est possible au plus tôt 20 heures avant l'événement.");
            if (hoursUntilEvent < 10)
                return BadRequest("La confirmation est impossible à moins de 10 heures de l'événement.");

            evenement.IsConfirmed = true;
            evenement.ConfirmedAt = DateTime.UtcNow;

            var participantIds = await _db.Participations
                .AsNoTracking()
                .Where(p => p.EvenementId == id && p.CitoyenId != citoyenId)
                .Select(p => p.CitoyenId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var participantId in participantIds)
            {
                _db.Notifications.Add(new Notification
                {
                    CitoyenId = participantId,
                    Type = "EVENEMENT_CONFIRME",
                    RelatedEvenementId = id,
                    Message =
                        $"Bonne nouvelle ! L'événement « {evenement.Titre} » est confirmé par l'organisateur. Rendez-vous le {evenement.DateEvenement:dd/MM/yyyy HH:mm} à {evenement.Lieu}.",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new { message = "Événement confirmé avec succès." });
        }

        public class UpdateParticipationCommentaireDto
        {
            public string? Commentaire { get; set; }
        }
    }
}
