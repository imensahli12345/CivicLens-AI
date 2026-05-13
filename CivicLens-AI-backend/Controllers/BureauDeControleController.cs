using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CivicLens.API.Controllers
{
    [Route("api/bureau")]
    [ApiController]
    //[Authorize(Roles = "BUREAUDECONTROLE")]
    public class BureauDeControleController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IBureauDeControleService _bureauService;
        private readonly ISignalementManagementService _signalementManagementService;
        private readonly IHistoriqueService _historiqueService;
        private readonly IOrganisationService _organisationService;

        public BureauDeControleController(
            IWebHostEnvironment env,
            IBureauDeControleService bureauService,
            ISignalementManagementService signalementManagementService,
            IHistoriqueService historiqueService,
            IOrganisationService organisationService)
        {
            _env = env;
            _bureauService = bureauService;
            _signalementManagementService = signalementManagementService;
            _historiqueService = historiqueService;
            _organisationService = organisationService;
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private async Task<BureauDeControle?> GetCurrentBureauAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                if (!_env.IsDevelopment())
                    return null;

                return await _bureauService.GetFirstBureauAsync(ct);
            }

            return await _bureauService.GetBureauByIdAsync(userId.Value, ct);
        }

        [HttpGet("signalements/tous")]
        public async Task<IActionResult> GetAllSignalements(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalements = await _signalementManagementService.GetAllSignalementsAsync(null, bureau.Delegation, ct);
            return Ok(signalements);
        }

        [HttpGet("signalements")]
        public async Task<IActionResult> GetSignalements([FromQuery] SignalementFilterDto filter, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalements = await _signalementManagementService.GetSignalementsAsync(null, bureau.Delegation, filter, ct);
                return Ok(signalements);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("signalements/{id:long}")]
        public async Task<IActionResult> GetSignalementById([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetSignalementByIdAsync(id, null, bureau.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre delegation.");

            return Ok(signalement);
        }

        [HttpGet("signalements/{id:long}/historique")]
        public async Task<IActionResult> GetHistorique([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetSignalementByIdAsync(id, null, bureau.Delegation, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable ou hors de votre delegation.");

            var historiques = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);
            return Ok(historiques);
        }

        [HttpPut("signalements/{id:long}/ouvrir")]
        public async Task<IActionResult> OuvrirSignalement([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.OuvrirSignalementAsync(id, null, bureau.Delegation, bureau.Id, ct);
                return Ok(new { message = "Signalement ouvert avec succes.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("signalements/{id:long}/envoyer")]
        public async Task<IActionResult> EnvoyerSignalement([FromRoute] long id, [FromBody] SendSignalementDto dto, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.EnvoyerSignalementAsync(id, null, bureau.Delegation, bureau.Id, dto.OrganisationId, dto.DescriptionSupplementaire, ct);
                var organisation = await _organisationService.GetByIdAsync(dto.OrganisationId, ct);
                return Ok(new
                {
                    message = "Signalement envoye avec succes a l'organisation.",
                    signalementId = signalement.Id,
                    statut = signalement.Statut.ToString(),
                    organisationId = signalement.OrganisationId,
                    organisationNom = organisation?.NomOrganisation
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("signalements/{id:long}/rejeter")]
        public async Task<IActionResult> RejeterSignalement([FromRoute] long id, CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.RejeterSignalementAsync(id, null, bureau.Delegation, bureau.Id, ct);
                return Ok(new { message = "Signalement rejete.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var stats = await _signalementManagementService.GetStatsAsync(null, bureau.Delegation, ct);
            return Ok(stats);
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            return Ok(new
            {
                id = bureau.Id,
                email = bureau.Email,
                region = bureau.Region,
                delegation = bureau.Delegation,
                matricule = bureau.Matricule,
                //statut = bureau.StatutCompte.ToString()
            });
        }

        [HttpGet("services")]
        public async Task<IActionResult> GetServices(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var services = await _bureauService.GetServicesAsync(bureau.Id, ct);
            return Ok(new
            {
                bureauId = bureau.Id,
                bureauEmail = bureau.Email,
                services = services.Select(service => new
                {
                    id = service.Id,
                    nom = service.Nom,
                    email = service.Email,
                    domaine = service.Domaine.ToString(),
                    statut = service.StatutCompte.ToString()
                })
            });
        }
[HttpPut("services/{serviceId:guid}/activer")]
public async Task<IActionResult> ActivateService([FromRoute] Guid serviceId, CancellationToken ct)
{
    var bureau = await GetCurrentBureauAsync(ct);
    if (bureau == null)
        return Unauthorized("Bureau de controle introuvable pour ce token.");

    try
    {
        await _bureauService.ActivateServiceAsync(bureau.Id, serviceId, ct);
        return Ok("Ce service est activé.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound("Service introuvable pour ce bureau.");
    }
    catch (InvalidOperationException)
    {
        // Status was not changed (e.g. already active, or wrong bureau)
        return NotFound("Le statut n'a pas pu être modifié, veuillez réessayer.");
    }
}

[HttpPut("services/{serviceId:guid}/desactiver")]
public async Task<IActionResult> DeactivateService([FromRoute] Guid serviceId, CancellationToken ct)
{
    var bureau = await GetCurrentBureauAsync(ct);
    if (bureau == null)
        return Unauthorized("Bureau de controle introuvable pour ce token.");

    try
    {
        await _bureauService.DeactivateServiceAsync(bureau.Id, serviceId, ct);
        return Ok("Ce service est désactivé.");
    }
    catch (KeyNotFoundException)
    {
        return NotFound("Service introuvable pour ce bureau.");
    }
    catch (InvalidOperationException)
    {
        return NotFound("Le statut n'a pas pu être modifié, veuillez réessayer.");
    }
}
        [HttpGet("organisations")]
        public async Task<IActionResult> GetOrganisations(CancellationToken ct)
        {
            var bureau = await GetCurrentBureauAsync(ct);
            if (bureau == null)
                return Unauthorized("Bureau de controle introuvable pour ce token.");

            var organisations = await _organisationService.GetListAsync(null, ct);
            return Ok(organisations.Select(o => new
            {
                id = o.Id,
                nomOrganisation = o.NomOrganisation,
                domaineOrganisation = o.DomaineOrganisation.ToString(),
                typeOrganisation = o.TypeOrganisation.ToString(),
                region = o.Region,
                //delegation = o.Delegation,
                email = o.Email
            }));
        }
    }
}
