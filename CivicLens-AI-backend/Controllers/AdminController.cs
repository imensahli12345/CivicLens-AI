using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Elements.Table;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;

namespace CivicLens.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "ADMIN,SUPERADMIN")]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IBureauDeControleService _bureauDeControleService;
        private readonly AppDbContext _db;

        public AdminController(IAuthService authService, IBureauDeControleService BureauDeControleService, AppDbContext db)
        {
            _authService = authService;
            _bureauDeControleService = BureauDeControleService;
            _db = db;
        }

        [HttpGet("citoyens")]
        public async Task<IActionResult> GetCitoyens([FromQuery] string? region = null, [FromQuery] string? delegation = null, [FromQuery] string? statut = null)
        {
            var query = _db.Citoyens.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(c => c.Region == region);

            if (!string.IsNullOrWhiteSpace(delegation))
                query = query.Where(c => c.Delegation == delegation);

            if (!string.IsNullOrWhiteSpace(statut))
            {
                if (!Enum.TryParse<StatutCompte>(statut, true, out var parsed))
                    return BadRequest("Statut invalide.");
                query = query.Where(c => c.StatutCompte == parsed);
            }

            var citoyens = await query.ToListAsync();
            return Ok(citoyens.Select(c => new {
                id = c.Id,
                nom = c.Nom,
                prenom = c.Prenom,
                email = c.Email,
                region = c.Region,
                delegation = c.Delegation,
                cin = c.Cin,
                telephone = c.Telephone,
                statut = c.StatutCompte.ToString(),
                dateCreation = c.DateCreation
            }));
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalCitoyens = await _db.Citoyens.CountAsync();
            var totalSignalements = await _db.Signalements.CountAsync();
            var totalOrganisations = await _db.Organisations.CountAsync();
            var totalBureaux = await _db.BureauxDeControle.CountAsync();
            var signalementsEnAttente = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_ATTENTE);
            var signalementsResolus = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.RESOLU);
            var signalementsEnCours = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_COURS_DETUDE);
            var signalementsRejetes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.REJETE);
            var signalementsEnvoyes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.ENVOYEE);

            return Ok(new {
                totalCitoyens,
                totalSignalements,
                totalOrganisations,
                totalBureaux,
                signalementsEnAttente,
                signalementsResolus,
                signalementsEnCours,
                signalementsRejetes,
                signalementsEnvoyes
            });
        }

        [HttpGet("stats/timeseries")]
        public async Task<IActionResult> GetStatsTimeseries([FromQuery] int days = 30)
        {
            if (days < 7) days = 7;
            if (days > 120) days = 120;

            var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var endExclusive = DateTime.UtcNow.Date.AddDays(1);

            var totalCitoyens = await _db.Citoyens.CountAsync();
            var totalSignalements = await _db.Signalements.CountAsync();
            var totalOrganisations = await _db.Organisations.CountAsync();
            var totalBureaux = await _db.BureauxDeControle.CountAsync();
            var signalementsEnAttente = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_ATTENTE);
            var signalementsResolus = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.RESOLU);
            var signalementsEnCours = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_COURS_DETUDE);
            var signalementsRejetes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.REJETE);
            var signalementsEnvoyes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.ENVOYEE);

            var signalementsCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var enAttenteCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.EN_ATTENTE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var enCoursCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.EN_COURS_DETUDE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var envoyeesCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.ENVOYEE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var rejetesCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.REJETE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var resolusResolved = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateResolution != null && s.DateResolution >= start && s.DateResolution < endExclusive)
                .GroupBy(s => s.DateResolution!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var citoyensCreated = await _db.Citoyens
                .AsNoTracking()
                .Where(c => c.DateCreation >= start && c.DateCreation < endExclusive)
                .GroupBy(c => c.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var bureauxCreated = await _db.BureauxDeControle
                .AsNoTracking()
                .Where(b => b.DateCreation >= start && b.DateCreation < endExclusive)
                .GroupBy(b => b.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var organisationsCreated = await _db.Organisations
                .AsNoTracking()
                .Where(o => o.DateCreation >= start && o.DateCreation < endExclusive)
                .GroupBy(o => o.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var dailyActive = await _db.Users
                .AsNoTracking()
                .Where(u => u.DernierLogin != null && u.DernierLogin >= start && u.DernierLogin < endExclusive)
                .GroupBy(u => u.DernierLogin!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var citoyenMap = citoyensCreated.ToDictionary(x => x.Date, x => x.Count);
            var bureauMap = bureauxCreated.ToDictionary(x => x.Date, x => x.Count);
            var orgMap = organisationsCreated.ToDictionary(x => x.Date, x => x.Count);
            var activeMap = dailyActive.ToDictionary(x => x.Date, x => x.Count);
            var signalementsMap = signalementsCreated.ToDictionary(x => x.Date, x => x.Count);
            var enAttenteMap = enAttenteCreated.ToDictionary(x => x.Date, x => x.Count);
            var enCoursMap = enCoursCreated.ToDictionary(x => x.Date, x => x.Count);
            var envoyeesMap = envoyeesCreated.ToDictionary(x => x.Date, x => x.Count);
            var rejetesMap = rejetesCreated.ToDictionary(x => x.Date, x => x.Count);
            var resolusMap = resolusResolved.ToDictionary(x => x.Date, x => x.Count);

            var dates = Enumerable.Range(0, days)
                .Select(i => start.AddDays(i))
                .ToList();

            var series = new
            {
                citoyens = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = citoyenMap.TryGetValue(d, out var v) ? v : 0 }),
                bureaux = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = bureauMap.TryGetValue(d, out var v) ? v : 0 }),
                organisations = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = orgMap.TryGetValue(d, out var v) ? v : 0 }),
                logins = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = activeMap.TryGetValue(d, out var v) ? v : 0 }),
                signalements = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = signalementsMap.TryGetValue(d, out var v) ? v : 0 }),
                enAttente = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = enAttenteMap.TryGetValue(d, out var v) ? v : 0 }),
                enCours = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = enCoursMap.TryGetValue(d, out var v) ? v : 0 }),
                envoyees = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = envoyeesMap.TryGetValue(d, out var v) ? v : 0 }),
                rejetes = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = rejetesMap.TryGetValue(d, out var v) ? v : 0 }),
                resolus = dates.Select(d => new { date = d.ToString("yyyy-MM-dd"), value = resolusMap.TryGetValue(d, out var v) ? v : 0 })
            };

            return Ok(new
            {
                totals = new
                {
                    totalCitoyens,
                    totalSignalements,
                    totalOrganisations,
                    totalBureaux,
                    signalementsEnAttente,
                    signalementsResolus,
                    signalementsEnCours,
                    signalementsRejetes,
                    signalementsEnvoyes
                },
                range = new { days, start = start.ToString("yyyy-MM-dd"), end = DateTime.UtcNow.Date.ToString("yyyy-MM-dd") },
                series
            });
        }

        [HttpGet("bureaux")]
        public async Task<IActionResult> GetBureaux([FromQuery] string? region = null, [FromQuery] string? delegation = null)
        {
            var query = _db.BureauxDeControle.Include(b => b.Services).AsQueryable();
            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(b => b.Region == region);
            if (!string.IsNullOrWhiteSpace(delegation))
                query = query.Where(b => b.Delegation == delegation);

            var bureaux = await query.ToListAsync();
            return Ok(bureaux.Select(b => new {
                id = b.Id,
                email = b.Email,
                region = b.Region,
                delegation = b.Delegation,
                matricule = b.Matricule,
                statut = b.StatutCompte.ToString(),
                services = b.Services.Select(s => new {
                    id = s.Id,
                    nom = s.Nom,
                    domaine = s.Domaine.ToString(),
                    statut = s.StatutCompte.ToString()
                })
            }));
        }

        [HttpGet("bureaux/{id:guid}")]
        public async Task<IActionResult> GetBureauById([FromRoute] Guid id, CancellationToken ct)
        {
            var bureau = await _db.BureauxDeControle
                .Include(b => b.Services)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id, ct);

            if (bureau == null)
                return NotFound("Bureau introuvable.");

            return Ok(new
            {
                id = bureau.Id,
                email = bureau.Email,
                region = bureau.Region,
                delegation = bureau.Delegation,
                matricule = bureau.Matricule,
                statut = bureau.StatutCompte.ToString(),
                dateCreation = bureau.DateCreation,
                services = bureau.Services.Select(s => new
                {
                    id = s.Id,
                    nom = s.Nom,
                    domaine = s.Domaine.ToString(),
                    statut = s.StatutCompte.ToString()
                })
            });
        }

        [HttpGet("organisations")]
        public async Task<IActionResult> GetOrganisations([FromQuery] string? domaine = null, [FromQuery] string? type = null)
        {
            var query = _db.Organisations.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(domaine))
            {
                if (!Enum.TryParse<Domaine>(domaine, true, out var parsedDomaine))
                    return BadRequest("Domaine invalide.");
                query = query.Where(o => o.DomaineOrganisation == parsedDomaine);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<TypeOrganisation>(type, true, out var parsedType))
                    return BadRequest("Type invalide.");
                query = query.Where(o => o.TypeOrganisation == parsedType);
            }

            var orgs = await query.ToListAsync();
            return Ok(orgs.Select(o => new {
                id = o.Id,
                email = o.Email,
                nomOrganisation = o.NomOrganisation,
                typeOrganisation = o.TypeOrganisation.ToString(),
                domaineOrganisation = o.DomaineOrganisation.ToString(),
                region = o.Region,
                delegation = o.Delegation,
                statut = o.StatutCompte.ToString(),
                dateCreation = o.DateCreation
            }));
        }

        [HttpGet("organisations/{id:guid}")]
        public async Task<IActionResult> GetOrganisationById([FromRoute] Guid id, CancellationToken ct)
        {
            var org = await _db.Organisations.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, ct);
            if (org == null)
                return NotFound("Organisation introuvable.");

            return Ok(new
            {
                id = org.Id,
                email = org.Email,
                nomOrganisation = org.NomOrganisation,
                typeOrganisation = org.TypeOrganisation.ToString(),
                domaineOrganisation = org.DomaineOrganisation.ToString(),
                region = org.Region,
                delegation = org.Delegation,
                statut = org.StatutCompte.ToString(),
                dateCreation = org.DateCreation
            });
        }

        [HttpPut("bureaux/{id:guid}")]
        public async Task<IActionResult> UpdateBureau([FromRoute] Guid id, [FromBody] UpdateBureauDto dto, CancellationToken ct)
        {
            var bureau = await _db.BureauxDeControle.Include(b => b.Services).FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bureau == null)
                return NotFound("Bureau introuvable.");

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                var emailUsed = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != id, ct);
                if (emailUsed)
                    return Conflict(new { message = "Cet email est déjà utilisé." });
                bureau.Email = email;
            }

            if (!string.IsNullOrWhiteSpace(dto.Matricule))
            {
                var matricule = dto.Matricule.Trim();
                var matriculeUsed = await _db.BureauxDeControle.AsNoTracking().AnyAsync(b => b.Matricule == matricule && b.Id != id, ct);
                if (matriculeUsed)
                    return Conflict(new { message = "Ce matricule est déjà utilisé." });
                bureau.Matricule = matricule;
            }

            if (!string.IsNullOrWhiteSpace(dto.Region))
                bureau.Region = dto.Region.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Delegation))
                bureau.Delegation = dto.Delegation.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NouveauMotDePasse))
            {
                var nextPwd = dto.NouveauMotDePasse.Trim();
                if (nextPwd.Length < 6)
                    return BadRequest("Nouveau mot de passe invalide (min 6 caractères).");

                var adminPwd = (dto.MotDePasseAdmin ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(adminPwd))
                    return BadRequest("Mot de passe admin requis.");

                var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
                    return Unauthorized("Token invalide.");

                var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminId, ct);
                if (adminUser == null)
                    return Unauthorized("Utilisateur introuvable.");

                if (!BCrypt.Net.BCrypt.Verify(adminPwd, adminUser.MotDePasseHash))
                    return BadRequest("Mot de passe admin incorrect.");

                bureau.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(nextPwd);
            }

            foreach (var s in bureau.Services)
            {
                s.Region = bureau.Region;
                s.Delegation = bureau.Delegation;
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                id = bureau.Id,
                email = bureau.Email,
                region = bureau.Region,
                delegation = bureau.Delegation,
                matricule = bureau.Matricule,
                statut = bureau.StatutCompte.ToString()
            });
        }

        [HttpPut("organisations/{id:guid}")]
        public async Task<IActionResult> UpdateOrganisation([FromRoute] Guid id, [FromBody] UpdateOrganisationDto dto, CancellationToken ct)
        {
            var org = await _db.Organisations.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (org == null)
                return NotFound("Organisation introuvable.");

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                var emailUsed = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == email && u.Id != id, ct);
                if (emailUsed)
                    return Conflict(new { message = "Cet email est déjà utilisé." });
                org.Email = email;
            }

            if (!string.IsNullOrWhiteSpace(dto.NomOrganisation))
                org.NomOrganisation = dto.NomOrganisation.Trim();

            if (!string.IsNullOrWhiteSpace(dto.TypeOrganisation))
            {
                if (!Enum.TryParse<TypeOrganisation>(dto.TypeOrganisation, true, out var parsedType))
                    return BadRequest("Type invalide.");
                org.TypeOrganisation = parsedType;
            }

            if (!string.IsNullOrWhiteSpace(dto.DomaineOrganisation))
            {
                if (!Enum.TryParse<Domaine>(dto.DomaineOrganisation, true, out var parsedDomaine))
                    return BadRequest("Domaine invalide.");
                org.DomaineOrganisation = parsedDomaine;
            }

            if (dto.Region != null)
                org.Region = dto.Region.Trim();

            if (dto.Delegation != null)
                org.Delegation = dto.Delegation.Trim();

            if (!string.IsNullOrWhiteSpace(dto.NouveauMotDePasse))
            {
                var nextPwd = dto.NouveauMotDePasse.Trim();
                if (nextPwd.Length < 6)
                    return BadRequest("Nouveau mot de passe invalide (min 6 caractères).");

                var adminPwd = (dto.MotDePasseAdmin ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(adminPwd))
                    return BadRequest("Mot de passe admin requis.");

                var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
                    return Unauthorized("Token invalide.");

                var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == adminId, ct);
                if (adminUser == null)
                    return Unauthorized("Utilisateur introuvable.");

                if (!BCrypt.Net.BCrypt.Verify(adminPwd, adminUser.MotDePasseHash))
                    return BadRequest("Mot de passe admin incorrect.");

                org.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(nextPwd);
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new
            {
                id = org.Id,
                email = org.Email,
                nomOrganisation = org.NomOrganisation,
                typeOrganisation = org.TypeOrganisation.ToString(),
                domaineOrganisation = org.DomaineOrganisation.ToString(),
                region = org.Region,
                delegation = org.Delegation,
                statut = org.StatutCompte.ToString()
            });
        }

        [HttpDelete("bureaux/{id:guid}")]
        public async Task<IActionResult> DeleteBureau([FromRoute] Guid id, CancellationToken ct)
        {
            var bureau = await _db.BureauxDeControle.Include(b => b.Services).FirstOrDefaultAsync(b => b.Id == id, ct);
            if (bureau == null)
                return NotFound("Bureau introuvable.");

            _db.Services.RemoveRange(bureau.Services);
            _db.BureauxDeControle.Remove(bureau);
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Bureau supprimé avec succès." });
        }

        [HttpDelete("organisations/{id:guid}")]
        public async Task<IActionResult> DeleteOrganisation([FromRoute] Guid id, CancellationToken ct)
        {
            var org = await _db.Organisations.FirstOrDefaultAsync(o => o.Id == id, ct);
            if (org == null)
                return NotFound("Organisation introuvable.");

            var hasSignalements = await _db.Signalements.AsNoTracking().AnyAsync(s => s.OrganisationId == id, ct);
            if (hasSignalements)
                return Conflict(new { message = "Impossible de supprimer cette organisation car elle est liée à des signalements. Désactivez-la plutôt ou réaffectez les signalements." });

            _db.Organisations.Remove(org);
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Organisation supprimée avec succès." });
        }

        [HttpGet("stats/pdf")]
        public async Task<IActionResult> DownloadStatsPdf([FromQuery] int days = 30, CancellationToken ct = default)
        {
            if (days < 7) days = 7;
            if (days > 120) days = 120;

            var start = DateTime.UtcNow.Date.AddDays(-(days - 1));
            var endExclusive = DateTime.UtcNow.Date.AddDays(1);

            var totalCitoyens = await _db.Citoyens.CountAsync(ct);
            var totalSignalements = await _db.Signalements.CountAsync(ct);
            var totalOrganisations = await _db.Organisations.CountAsync(ct);
            var totalBureaux = await _db.BureauxDeControle.CountAsync(ct);
            var signalementsEnAttente = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_ATTENTE, ct);
            var signalementsResolus = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.RESOLU, ct);
            var signalementsEnCours = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.EN_COURS_DETUDE, ct);
            var signalementsRejetes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.REJETE, ct);
            var signalementsEnvoyes = await _db.Signalements.CountAsync(s => s.Statut == StatutSignalement.ENVOYEE, ct);

            var dates = Enumerable.Range(0, days).Select(i => start.AddDays(i)).ToList();

            var citoyensCreated = await _db.Citoyens
                .AsNoTracking()
                .Where(c => c.DateCreation >= start && c.DateCreation < endExclusive)
                .GroupBy(c => c.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var bureauxCreated = await _db.BureauxDeControle
                .AsNoTracking()
                .Where(b => b.DateCreation >= start && b.DateCreation < endExclusive)
                .GroupBy(b => b.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var organisationsCreated = await _db.Organisations
                .AsNoTracking()
                .Where(o => o.DateCreation >= start && o.DateCreation < endExclusive)
                .GroupBy(o => o.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var dailyActive = await _db.Users
                .AsNoTracking()
                .Where(u => u.DernierLogin != null && u.DernierLogin >= start && u.DernierLogin < endExclusive)
                .GroupBy(u => u.DernierLogin!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var signalementsCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var enAttenteCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.EN_ATTENTE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var enCoursCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.EN_COURS_DETUDE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var envoyeesCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.ENVOYEE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var rejetesCreated = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateCreation >= start && s.DateCreation < endExclusive && s.Statut == StatutSignalement.REJETE)
                .GroupBy(s => s.DateCreation.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var resolusResolved = await _db.Signalements
                .AsNoTracking()
                .Where(s => s.DateResolution != null && s.DateResolution >= start && s.DateResolution < endExclusive)
                .GroupBy(s => s.DateResolution!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            var citoyenMap = citoyensCreated.ToDictionary(x => x.Date, x => x.Count);
            var bureauMap = bureauxCreated.ToDictionary(x => x.Date, x => x.Count);
            var orgMap = organisationsCreated.ToDictionary(x => x.Date, x => x.Count);
            var activeMap = dailyActive.ToDictionary(x => x.Date, x => x.Count);
            var signalementsMap = signalementsCreated.ToDictionary(x => x.Date, x => x.Count);
            var enAttenteMap = enAttenteCreated.ToDictionary(x => x.Date, x => x.Count);
            var enCoursMap = enCoursCreated.ToDictionary(x => x.Date, x => x.Count);
            var envoyeesMap = envoyeesCreated.ToDictionary(x => x.Date, x => x.Count);
            var rejetesMap = rejetesCreated.ToDictionary(x => x.Date, x => x.Count);
            var resolusMap = resolusResolved.ToDictionary(x => x.Date, x => x.Count);

            var rows = dates.Select(d => new
            {
                Date = d,
                Citoyens = citoyenMap.TryGetValue(d, out var c) ? c : 0,
                Bureaux = bureauMap.TryGetValue(d, out var b) ? b : 0,
                Orgs = orgMap.TryGetValue(d, out var o) ? o : 0,
                Logins = activeMap.TryGetValue(d, out var l) ? l : 0,
                Signalements = signalementsMap.TryGetValue(d, out var s) ? s : 0,
                EnAttente = enAttenteMap.TryGetValue(d, out var ea) ? ea : 0,
                EnCours = enCoursMap.TryGetValue(d, out var ec) ? ec : 0,
                Envoyees = envoyeesMap.TryGetValue(d, out var ev) ? ev : 0,
                Rejetes = rejetesMap.TryGetValue(d, out var rej) ? rej : 0,
                Resolus = resolusMap.TryGetValue(d, out var r) ? r : 0
            }).ToList();

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(12);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(9).FontColor("#0f172a"));

                    page.Header()
                        .Background("#0ea5e9")
                        .PaddingHorizontal(16)
                        .PaddingVertical(10)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CivicLens").FontSize(14).FontColor(Colors.White).SemiBold();
                                col.Item().Text("Rapport Statistiques Application").FontSize(11).FontColor(Colors.White);
                                col.Item().PaddingTop(2).Text($"Période: {days} jours").FontSize(9).FontColor(Colors.White);
                            });
                            row.ConstantItem(240).AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text($"Généré: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.White);
                                col.Item().AlignRight().Text($"Du {start:dd/MM/yyyy} au {DateTime.UtcNow.Date:dd/MM/yyyy}").FontSize(9).FontColor(Colors.White);
                            });
                        });

                    page.Content().Background("#f8fafc").Padding(8).Column(content =>
                    {
                        content.Spacing(6);

                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Citoyens", totalCitoyens.ToString(), "#3b82f6"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Bureaux", totalBureaux.ToString(), "#10b981"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Organisations", totalOrganisations.ToString(), "#f97316"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Signalements", totalSignalements.ToString(), "#8b5cf6"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "En attente", signalementsEnAttente.ToString(), "#f59e0b"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Résolus", signalementsResolus.ToString(), "#14b8a6"));
                        });

                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "En cours", signalementsEnCours.ToString(), "#f59e0b"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Envoyés", signalementsEnvoyes.ToString(), "#2563eb"));
                            row.ConstantItem(8);
                            row.RelativeItem().Element(e => BuildInfoCardCompact(e, "Rejetés", signalementsRejetes.ToString(), "#dc2626"));
                        });

                        content.Item().Element(card =>
                        {
                            card.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(8).Column(col =>
                            {
                                col.Item().Text("Séries journalières").FontSize(10).SemiBold();
                                col.Item().PaddingTop(4).LineHorizontal(2).LineColor("#0ea5e9");

                                col.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(56);
                                        columns.ConstantColumn(62);
                                        columns.ConstantColumn(62);
                                        columns.ConstantColumn(82);
                                        columns.ConstantColumn(62);
                                        columns.ConstantColumn(78);
                                        columns.ConstantColumn(66);
                                        columns.ConstantColumn(66);
                                        columns.ConstantColumn(66);
                                        columns.ConstantColumn(66);
                                        columns.ConstantColumn(62);
                                    });

                                    table.Header(header =>
                                    {
                                        HeaderCellCompact(header.Cell(), "Date");
                                        HeaderCellCompact(header.Cell(), "Citoyens");
                                        HeaderCellCompact(header.Cell(), "Bureaux");
                                        HeaderCellCompact(header.Cell(), "Organisations");
                                        HeaderCellCompact(header.Cell(), "Logins");
                                        HeaderCellCompact(header.Cell(), "Signalements");
                                        HeaderCellCompact(header.Cell(), "En attente");
                                        HeaderCellCompact(header.Cell(), "En cours");
                                        HeaderCellCompact(header.Cell(), "Envoyées");
                                        HeaderCellCompact(header.Cell(), "Rejetés");
                                        HeaderCellCompact(header.Cell(), "Résolus");
                                    });

                                    foreach (var r in rows.TakeLast(12))
                                    {
                                        BodyCellCompact(table.Cell(), r.Date.ToString("dd/MM"));
                                        BodyCellCompact(table.Cell(), r.Citoyens.ToString());
                                        BodyCellCompact(table.Cell(), r.Bureaux.ToString());
                                        BodyCellCompact(table.Cell(), r.Orgs.ToString());
                                        BodyCellCompact(table.Cell(), r.Logins.ToString());
                                        BodyCellCompact(table.Cell(), r.Signalements.ToString());
                                        BodyCellCompact(table.Cell(), r.EnAttente.ToString());
                                        BodyCellCompact(table.Cell(), r.EnCours.ToString());
                                        BodyCellCompact(table.Cell(), r.Envoyees.ToString());
                                        BodyCellCompact(table.Cell(), r.Rejetes.ToString());
                                        BodyCellCompact(table.Cell(), r.Resolus.ToString());
                                    }
                                });
                            });
                        });
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"rapport_statistiques_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        [HttpGet("organisations/pdf")]
        public async Task<IActionResult> DownloadOrganisationsPdf([FromQuery] string? domaine = null, [FromQuery] string? type = null, [FromQuery] string? q = null, CancellationToken ct = default)
        {
            var query = _db.Organisations.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(domaine))
            {
                if (!Enum.TryParse<Domaine>(domaine, true, out var parsedDomaine))
                    return BadRequest("Domaine invalide.");
                query = query.Where(o => o.DomaineOrganisation == parsedDomaine);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!Enum.TryParse<TypeOrganisation>(type, true, out var parsedType))
                    return BadRequest("Type invalide.");
                query = query.Where(o => o.TypeOrganisation == parsedType);
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim().ToLower();
                query = query.Where(o => o.NomOrganisation.ToLower().Contains(needle) || o.Email.ToLower().Contains(needle));
            }

            var orgs = await query.OrderBy(o => o.NomOrganisation).ToListAsync(ct);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10).FontColor("#0f172a"));

                    page.Header()
                        .Background("#0ea5e9")
                        .PaddingHorizontal(16)
                        .PaddingVertical(12)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CivicLens").FontSize(16).FontColor(Colors.White).SemiBold();
                                col.Item().Text("Rapport Organisations").FontSize(12).FontColor(Colors.White);
                                col.Item().PaddingTop(2).Text($"Total: {orgs.Count}").FontSize(10).FontColor(Colors.White);
                            });
                            row.ConstantItem(320).AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text($"Généré: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.White);
                                col.Item().AlignRight().Text($"Filtres: domaine={domaine ?? "TOUS"} • type={type ?? "TOUS"}").FontSize(10).FontColor(Colors.White);
                                col.Item().AlignRight().Text($"Recherche: {(string.IsNullOrWhiteSpace(q) ? "—" : q)}").FontSize(10).FontColor(Colors.White);
                            });
                        });

                    page.Content().Background("#f8fafc").Padding(12).Column(content =>
                    {
                        content.Spacing(10);

                        content.Item().Element(card =>
                        {
                            card.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(10).Column(col =>
                            {
                                col.Item().Text("Liste").FontSize(12).SemiBold();
                                col.Item().PaddingTop(6).LineHorizontal(2).LineColor("#0ea5e9");

                                col.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                    });

                                    table.Header(header =>
                                    {
                                        HeaderCell(header.Cell(), "Nom");
                                        HeaderCell(header.Cell(), "Email");
                                        HeaderCell(header.Cell(), "Type");
                                        HeaderCell(header.Cell(), "Domaine");
                                        HeaderCell(header.Cell(), "Région");
                                        HeaderCell(header.Cell(), "Statut");
                                        HeaderCell(header.Cell(), "Création");
                                    });

                                    foreach (var o in orgs.Take(22))
                                    {
                                        BodyCell(table.Cell(), o.NomOrganisation);
                                        BodyCell(table.Cell(), o.Email);
                                        BodyCell(table.Cell(), o.TypeOrganisation.ToString());
                                        BodyCell(table.Cell(), o.DomaineOrganisation.ToString());
                                        BodyCell(table.Cell(), string.IsNullOrWhiteSpace(o.Region) ? "—" : o.Region);
                                        BodyCell(table.Cell(), o.StatutCompte.ToString());
                                        BodyCell(table.Cell(), o.DateCreation.ToString("dd/MM/yyyy"));
                                    }

                                    if (orgs.Count > 22)
                                    {
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), $"(+{orgs.Count - 22} autres)");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                    }
                                });
                            });
                        });
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"rapport_organisations_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        [HttpGet("bureaux/pdf")]
        public async Task<IActionResult> DownloadBureauxPdf([FromQuery] string? region = null, [FromQuery] string? delegation = null, CancellationToken ct = default)
        {
            var query = _db.BureauxDeControle.Include(b => b.Services).AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(region))
                query = query.Where(b => b.Region == region);
            if (!string.IsNullOrWhiteSpace(delegation))
                query = query.Where(b => b.Delegation == delegation);

            var bureaux = await query.OrderBy(b => b.Region).ThenBy(b => b.Delegation).ToListAsync(ct);

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(18);
                    page.DefaultTextStyle(x => x.FontFamily("Helvetica").FontSize(10).FontColor("#0f172a"));

                    page.Header()
                        .Background("#0ea5e9")
                        .PaddingHorizontal(16)
                        .PaddingVertical(12)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("CivicLens").FontSize(16).FontColor(Colors.White).SemiBold();
                                col.Item().Text("Rapport Bureaux de Contrôle").FontSize(12).FontColor(Colors.White);
                                col.Item().PaddingTop(2).Text($"Total: {bureaux.Count}").FontSize(10).FontColor(Colors.White);
                            });
                            row.ConstantItem(320).AlignRight().Column(col =>
                            {
                                col.Item().AlignRight().Text($"Généré: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.White);
                                col.Item().AlignRight().Text($"Filtres: région={region ?? "TOUS"} • délégation={delegation ?? "TOUS"}").FontSize(10).FontColor(Colors.White);
                            });
                        });

                    page.Content().Background("#f8fafc").Padding(12).Column(content =>
                    {
                        content.Spacing(10);

                        content.Item().Element(card =>
                        {
                            card.Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.White).Padding(10).Column(col =>
                            {
                                col.Item().Text("Liste").FontSize(12).SemiBold();
                                col.Item().PaddingTop(6).LineHorizontal(2).LineColor("#0ea5e9");

                                col.Item().PaddingTop(8).Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn(1);
                                    });

                                    table.Header(header =>
                                    {
                                        HeaderCell(header.Cell(), "Email");
                                        HeaderCell(header.Cell(), "Matricule");
                                        HeaderCell(header.Cell(), "Région");
                                        HeaderCell(header.Cell(), "Délégation");
                                        HeaderCell(header.Cell(), "Statut");
                                        HeaderCell(header.Cell(), "Services");
                                    });

                                    foreach (var b in bureaux.Take(22))
                                    {
                                        BodyCell(table.Cell(), b.Email);
                                        BodyCell(table.Cell(), b.Matricule);
                                        BodyCell(table.Cell(), b.Region);
                                        BodyCell(table.Cell(), b.Delegation);
                                        BodyCell(table.Cell(), b.StatutCompte.ToString());
                                        BodyCell(table.Cell(), (b.Services?.Count ?? 0).ToString());
                                    }

                                    if (bureaux.Count > 22)
                                    {
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), $"(+{bureaux.Count - 22} autres)");
                                        BodyCell(table.Cell(), "…");
                                        BodyCell(table.Cell(), "…");
                                    }
                                });
                            });
                        });
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"rapport_bureaux_{DateTime.Now:yyyyMMdd_HHmm}.pdf");
        }

        [HttpPut("bureaux/{id:guid}/statut")]
        public async Task<IActionResult> UpdateBureauStatut([FromRoute] Guid id, [FromBody] UpdateAccountStatusDto dto)
        {
            if (!Enum.TryParse<StatutCompte>(dto.Statut, true, out var parsed))
                return BadRequest("Statut invalide.");

            if (parsed != StatutCompte.ACTIF && parsed != StatutCompte.INACTIF)
                return BadRequest("Statut non autorisé.");

            var bureau = await _db.BureauxDeControle.FindAsync(id);
            if (bureau == null) return NotFound("Bureau introuvable.");

            bureau.StatutCompte = parsed;
            await _db.SaveChangesAsync();
            return Ok(new { statut = bureau.StatutCompte.ToString() });
        }

        [HttpPut("organisations/{id:guid}/statut")]
        public async Task<IActionResult> UpdateOrganisationStatut([FromRoute] Guid id, [FromBody] UpdateAccountStatusDto dto)
        {
            if (!Enum.TryParse<StatutCompte>(dto.Statut, true, out var parsed))
                return BadRequest("Statut invalide.");

            if (parsed != StatutCompte.ACTIF && parsed != StatutCompte.INACTIF)
                return BadRequest("Statut non autorisé.");

            var org = await _db.Organisations.FindAsync(id);
            if (org == null) return NotFound("Organisation introuvable.");

            org.StatutCompte = parsed;
            await _db.SaveChangesAsync();
            return Ok(new { statut = org.StatutCompte.ToString() });
        }

        [HttpPost("create-admin")]
        public IActionResult CreateAdmin([FromBody] CreateAdminDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
                return BadRequest("Cet email est déjà utilisé.");

            var admin = new User
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                Region = dto.Region,
                Delegation = dto.Delegation
            };

            var createdAdmin = _authService.CreateAdmin(admin);
            if (createdAdmin != null) return Ok("Admin créé avec succès.");
            return BadRequest("Échec de la création de l'admin.");
        }

        [HttpPost("create-bureau")]
        public IActionResult CreateBureau([FromBody] CreateBureauDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
                return BadRequest("Cet email est déjà utilisé.");

            if (_authService.IsMatriculeExists(dto.Matricule))
                return BadRequest("Ce matricule est déjà utilisé.");

            var bureau = new BureauDeControle
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                Delegation = dto.Delegation,
                Matricule = dto.Matricule,
                Region = dto.Region
            };

            var createdBureau = _authService.CreateBureau(bureau);
            if (createdBureau != null) return Ok("Bureau de contrôle créé avec succès.");
            return BadRequest("Échec de la création du bureau de contrôle.");
        }

        [HttpPost("create-organisation")]
        public IActionResult CreateOrganisation([FromBody] CreateOrganisationDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
                return BadRequest("Cet email est déjà utilisé.");

            if (!Enum.TryParse<TypeOrganisation>(dto.TypeOrganisation, true, out var typeOrg))
                return BadRequest($"Type d'organisation invalide : {dto.TypeOrganisation}");

            if (!Enum.TryParse<Domaine>(dto.DomaineOrganisation, true, out var domaine))
                return BadRequest($"Domaine invalide : {dto.DomaineOrganisation}");

            var organisation = new Organisation
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                NomOrganisation = dto.NomOrganisation,
                TypeOrganisation = typeOrg,
                DomaineOrganisation = domaine,
                Region = dto.Region
            };

            var createdOrg = _authService.CreateOrganisation(organisation);
            if (createdOrg != null) return Ok("Organisation créée avec succès.");
            return BadRequest("Échec de la création de l'organisation.");
        }

        [HttpPost("create-service")]
        public IActionResult CreateService([FromBody] CreateServiceDto dto)
        {
            if (_bureauDeControleService.GetBureauById(dto.BureauDeControleId) is not BureauDeControle bureau)
                return BadRequest("Bureau de contrôle non trouvé pour l'ID fourni.");

            if (!Enum.TryParse<Domaine>(dto.Domaine, true, out var domaine))
                return BadRequest($"Domaine invalide : {dto.Domaine}");

            var service = new Service
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                Delegation = dto.Delegation,
                Nom = dto.Nom,
                Region = dto.Region,
                Domaine = domaine,
                BureauDeControleId = bureau.Id,
                BureauDeControle = bureau
            };

            _bureauDeControleService.CreateService(service);
            return Ok("Service créé avec succès.");
        }

        private static void BuildInfoCard(IContainer container, string label, string value, string accentHex)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Row(row =>
                {
                    row.ConstantItem(4).Background(accentHex);
                    row.RelativeItem().Padding(10).Column(col =>
                    {
                        col.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(2).Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(11).FontColor("#0f172a").SemiBold();
                    });
                });
        }

        private static void BuildInfoCardCompact(IContainer container, string label, string value, string accentHex)
        {
            container
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Background(Colors.White)
                .Row(row =>
                {
                    row.ConstantItem(4).Background(accentHex);
                    row.RelativeItem().Padding(8).Column(col =>
                    {
                        col.Item().Text(label).FontSize(7).FontColor(Colors.Grey.Darken1);
                        col.Item().PaddingTop(1).Text(string.IsNullOrWhiteSpace(value) ? "—" : value).FontSize(10).FontColor("#0f172a").SemiBold();
                    });
                });
        }

        private static void HeaderCell(ITableCellContainer cell, string text)
        {
            cell.Element(c => c.PaddingVertical(4).Text(text).FontSize(9).FontColor(Colors.Grey.Darken2).SemiBold());
        }

        private static void BodyCell(ITableCellContainer cell, string text)
        {
            cell.Element(c => c.PaddingVertical(3).Text(string.IsNullOrWhiteSpace(text) ? "—" : text).FontSize(9).FontColor("#0f172a"));
        }

        private static void HeaderCellCompact(ITableCellContainer cell, string text)
        {
            cell.Element(c => c.PaddingVertical(2).Text(text).FontSize(8).FontColor(Colors.Grey.Darken2).SemiBold());
        }

        private static void BodyCellCompact(ITableCellContainer cell, string text)
        {
            cell.Element(c => c.PaddingVertical(2).Text(string.IsNullOrWhiteSpace(text) ? "—" : text).FontSize(8).FontColor("#0f172a"));
        }

        public class UpdateBureauDto
        {
            public string? Email { get; set; }
            public string? Matricule { get; set; }
            public string? Region { get; set; }
            public string? Delegation { get; set; }
            public string? NouveauMotDePasse { get; set; }
            public string? MotDePasseAdmin { get; set; }
        }


        public class UpdateOrganisationDto
        {
            public string? Email { get; set; }
            public string? NomOrganisation { get; set; }
            public string? TypeOrganisation { get; set; }
            public string? DomaineOrganisation { get; set; }
            public string? Region { get; set; }
            public string? Delegation { get; set; }
            public string? NouveauMotDePasse { get; set; }
            public string? MotDePasseAdmin { get; set; }
        }

        [HttpGet("reclamations")]
        public async Task<IActionResult> GetPlatformReclamations([FromQuery] string? status = null, CancellationToken ct = default)
        {
            IQueryable<PlatformReclamation> q = _db.PlatformReclamations.AsNoTracking().OrderByDescending(r => r.CreatedAt);
            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(r => r.Status == status.Trim().ToUpper());

            var list = await q.Take(500).ToListAsync(ct);
            return Ok(list.Select(r => new
            {
                id = r.Id,
                createdAt = r.CreatedAt,
                fromUserId = r.FromUserId,
                fromEmail = r.FromEmail,
                subject = r.Subject,
                message = r.Message,
                appVersion = r.AppVersion,
                context = r.Context,
                status = r.Status
            }));
        }

        [HttpPut("reclamations/{id:guid}/status")]
        public async Task<IActionResult> UpdateReclamationStatus([FromRoute] Guid id, [FromBody] UpdateReclamationStatusDto dto, CancellationToken ct)
        {
            var next = (dto.Status ?? string.Empty).Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(next))
                return BadRequest("Status requis.");

            var rec = await _db.PlatformReclamations.FirstOrDefaultAsync(r => r.Id == id, ct);
            if (rec == null) return NotFound();

            rec.Status = next;
            await _db.SaveChangesAsync(ct);
            return Ok(new { status = rec.Status });
        }

        [HttpGet("activation-requests")]
        public async Task<IActionResult> GetActivationRequests(CancellationToken ct)
        {
            var adminIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
                return Unauthorized("Token invalide.");

            var list = await (
                from n in _db.Notifications.AsNoTracking()
                where n.CitoyenId == adminId && (n.Type == "ACTIVATION_REQUEST" || n.Type == "ACTIVATION_REQUEST_SERVICE")
                join u in _db.Users.AsNoTracking() on n.RelatedUserId equals u.Id into userJoin
                from u in userJoin.DefaultIfEmpty()
                orderby n.CreatedAt descending
                select new
                {
                    id = n.Id,
                    type = n.Type,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead,
                    relatedUserId = n.RelatedUserId,
                    userStatut = u == null ? null : u.StatutCompte.ToString(),
                    approved = u != null && u.StatutCompte == StatutCompte.ACTIF
                }
            )
            .Take(500)
            .ToListAsync(ct);

            return Ok(list);
        }

        [HttpPost("activation-requests/{notificationId:guid}/approve")]
        public async Task<IActionResult> ApproveActivationRequest([FromRoute] Guid notificationId, CancellationToken ct)
        {
            var adminIdStr = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrWhiteSpace(adminIdStr) || !Guid.TryParse(adminIdStr, out var adminId))
                return Unauthorized("Token invalide.");

            var notif = await _db.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.CitoyenId == adminId, ct);
            if (notif == null) return NotFound();

            if (notif.RelatedUserId == null) return BadRequest("Demande invalide.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == notif.RelatedUserId.Value, ct);
            if (user == null) return NotFound("Utilisateur introuvable.");

            if (user.StatutCompte == StatutCompte.ACTIF)
            {
                notif.IsRead = true;
                await _db.SaveChangesAsync(ct);
                return Ok(new { message = "Compte déjà actif.", userId = user.Id, alreadyActive = true });
            }

            user.StatutCompte = StatutCompte.ACTIF;
            notif.IsRead = true;
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Compte activé.", userId = user.Id, alreadyActive = false });
        }

        public class UpdateReclamationStatusDto
        {
            public string Status { get; set; } = string.Empty;
        }

        [HttpPut("me/password")]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangeMyPasswordDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.AncienMotDePasse))
                return BadRequest("Ancien mot de passe requis.");

            if (string.IsNullOrWhiteSpace(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Trim().Length < 6)
                return BadRequest("Nouveau mot de passe invalide (min 6 caractères).");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user == null)
                return NotFound("Utilisateur introuvable.");

            if (user.Role != Role.ADMIN && user.Role != Role.SUPERADMIN)
                return Forbid();

            if (!BCrypt.Net.BCrypt.Verify(dto.AncienMotDePasse, user.MotDePasseHash))
                return BadRequest("Ancien mot de passe incorrect.");

            user.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse.Trim());
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Mot de passe mis à jour avec succès." });
        }

        public class ChangeMyPasswordDto
        {
            public string AncienMotDePasse { get; set; } = string.Empty;
            public string NouveauMotDePasse { get; set; } = string.Empty;
        }
    }
}
