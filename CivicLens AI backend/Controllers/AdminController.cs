using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CivicLens.API.Controllers
{
    /// <summary>
    /// Contrôleur d'administration — endpoints protégés par rôle.
    /// 
    /// [Authorize] → nécessite un token JWT valide.
    /// [Authorize(Roles = "SUPERADMIN")] → seul le SUPERADMIN peut accéder.
    /// [Authorize(Roles = "ADMIN")] → seul l'ADMIN peut accéder.
    /// 
    /// Le middleware JWT (dans Program.cs) extrait le rôle du token
    /// et le compare avec l'attribut [Authorize(Roles = "...")].
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IBureauDeControleService _bureauDeControleService;

        public AdminController(IAuthService authService, IBureauDeControleService BureauDeControleService)
        {
            _authService = authService;
            _bureauDeControleService = BureauDeControleService;
        }

        // ──────────────────────────────────────────────────────
        // POST /api/admin/create-admin
        // Accès : SUPERADMIN uniquement
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [Route("create-admin")]
        [Authorize(Roles = "SUPERADMIN")]
        public IActionResult CreateAdmin([FromBody] CreateAdminDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
            {
                return BadRequest("Cet email est déjà utilisé.");
            }

            var admin = new User
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,  // Le Repository va hasher avec BCrypt
                Region = dto.Region,
                Delegation = dto.Delegation
            };

            var createdAdmin = _authService.CreateAdmin(admin);

            if (createdAdmin != null)
            {
                return Ok("Admin créé avec succès.");
            }

            return BadRequest("Échec de la création de l'admin.");
        }

        // ──────────────────────────────────────────────────────
        // POST /api/admin/create-bureau
        // Accès : ADMIN uniquement
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [Route("create-bureau")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult CreateBureau([FromBody] CreateBureauDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
            {
                return BadRequest("Cet email est déjà utilisé.");
            }

            if (_authService.IsMatriculeExists(dto.Matricule))
            {
                return BadRequest("Ce matricule est déjà utilisé.");
            }

            var bureau = new BureauDeControle
            {
                Email = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                Delegation = dto.Delegation,
                Matricule = dto.Matricule,
                Region = dto.Region
            };

            var createdBureau = _authService.CreateBureau(bureau);

            if (createdBureau != null)
            {
                return Ok("Bureau de contrôle créé avec succès.");
            }

            return BadRequest("Échec de la création du bureau de contrôle.");
        }

        // ──────────────────────────────────────────────────────
        // POST /api/admin/create-organisation
        // Accès : ADMIN uniquement
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [Route("create-organisation")]
        [Authorize(Roles = "ADMIN")]
        public IActionResult CreateOrganisation([FromBody] CreateOrganisationDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
            {
                return BadRequest("Cet email est déjà utilisé.");
            }

            // Parser les enums depuis les strings envoyées par le client
            if (!Enum.TryParse<TypeOrganisation>(dto.TypeOrganisation, true, out var typeOrg))
            {
                return BadRequest($"Type d'organisation invalide : {dto.TypeOrganisation}");
            }

            if (!Enum.TryParse<Domaine>(dto.DomaineOrganisation, true, out var domaine))
            {
                return BadRequest($"Domaine invalide : {dto.DomaineOrganisation}");
            }

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

            if (createdOrg != null)
            {
                return Ok("Organisation créée avec succès.");
            }

            return BadRequest("Échec de la création de l'organisation.");
        }

        // ──────────────────────────────────────────────────────
        // POST /api/admin/create-service
        // Accès : ADMIN uniquement
        // ──────────────────────────────────────────────────────
        [HttpPost]
        [Route("create-service")]
        [Authorize(Roles = "ADMIN")]
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

            var createdService = _bureauDeControleService.CreateService(service);
            return Ok("Service créé avec succès.");
        }

    }
}
