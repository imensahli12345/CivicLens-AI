using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            return Ok(new {
                totalCitoyens,
                totalSignalements,
                totalOrganisations,
                totalBureaux,
                signalementsEnAttente,
                signalementsResolus
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
                    signalementsResolus
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
    }
}
