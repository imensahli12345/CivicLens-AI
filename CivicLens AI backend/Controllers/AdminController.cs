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
    [Authorize]
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
        [Authorize]
        public async Task<IActionResult> GetCitoyens()
        {
            var citoyens = await _db.Citoyens.ToListAsync();
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

        [HttpPut("citoyens/{id}/statut")]
        [Authorize]
        public async Task<IActionResult> UpdateCitoyenStatut(Guid id, [FromBody] string statut)
        {
            var citoyen = await _db.Citoyens.FindAsync(id);
            if (citoyen == null) return NotFound("Citoyen introuvable.");

            if (!Enum.TryParse<StatutCompte>(statut, true, out var newStatut))
                return BadRequest("Statut invalide.");

            citoyen.StatutCompte = newStatut;
            await _db.SaveChangesAsync();
            return Ok("Statut mis à jour.");
        }

        [HttpGet("stats")]
        [Authorize]
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

        [HttpGet("bureaux")]
        [Authorize]
        public async Task<IActionResult> GetBureaux()
        {
            var bureaux = await _db.BureauxDeControle.Include(b => b.Services).ToListAsync();
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

        [HttpPost("create-admin")]
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
                Delegation = dto.Delegation,
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
        [Authorize]
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