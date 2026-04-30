using CivicLens.API.Models.DTOs;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CivicLens.API.Controllers
{
    [Route("api/organisation")]
    [ApiController]
    //[Authorize(Roles = "ORGANISATION")]
    public class OrganisationController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly IOrganisationService _organisationService;
        private readonly ISignalementManagementService _signalementManagementService;
        private readonly IHistoriqueService _historiqueService;

        public OrganisationController(
            IWebHostEnvironment env,
            IOrganisationService organisationService,
            ISignalementManagementService signalementManagementService,
            IHistoriqueService historiqueService)
        {
            _env = env;
            _organisationService = organisationService;
            _signalementManagementService = signalementManagementService;
            _historiqueService = historiqueService;
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }

        private async Task<CivicLens.API.Models.Entities.Organisation?> GetCurrentOrganisationAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                if (!_env.IsDevelopment())
                    return null;
                return await _organisationService.GetFirstAsync(ct);
            }
            return await _organisationService.GetByIdAsync(userId.Value, ct);
        }

        [HttpGet("signalements/tous")]
        public async Task<IActionResult> GetAllSignalements(CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            var signalements = await _signalementManagementService.GetAllOrganisationSignalementsAsync(organisation.Id, ct);
            return Ok(signalements);
        }

        // GET filtered
        [HttpGet("signalements")]
        public async Task<IActionResult> GetSignalements([FromQuery] SignalementFilterDto filter, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            try
            {
                var signalements = await _signalementManagementService.GetOrganisationSignalementsAsync(organisation.Id, filter, ct);
                return Ok(signalements);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET by ID
        [HttpGet("signalements/{id:long}")]
        public async Task<IActionResult> GetSignalementById([FromRoute] long id, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetOrganisationSignalementByIdAsync(id, organisation.Id, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable pour cette organisation.");

            return Ok(signalement);
        }

        // GET historique (separate endpoint)
        [HttpGet("signalements/{id:long}/historique")]
        public async Task<IActionResult> GetHistorique([FromRoute] long id, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            var signalement = await _signalementManagementService.GetOrganisationSignalementByIdAsync(id, organisation.Id, ct);
            if (signalement == null)
                return NotFound("Signalement introuvable pour cette organisation.");

            var historiques = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);
            return Ok(historiques);
        }

        [HttpPut("signalements/{id:long}/ouvrir")]
        public async Task<IActionResult> OuvrirSignalement([FromRoute] long id, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.CommencerEtudeOrganisationAsync(id, organisation.Id, organisation.Id, ct);
                return Ok(new { message = "Signalement passe en cours d'etude.", signalementId = signalement.Id, statut = signalement.Statut.ToString() });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("signalements/{id:long}/resoudre")]
        public async Task<IActionResult> ResoudreSignalement([FromRoute] long id, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            try
            {
                var signalement = await _signalementManagementService.ResoudreSignalementOrganisationAsync(id, organisation.Id, organisation.Id, ct);
                return Ok(new
                {
                    message = "Signalement resolu avec succes.",
                    signalementId = signalement.Id,
                    statut = signalement.Statut.ToString(),
                    dateResolution = signalement.DateResolution
                });
            }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("signalements/{id:long}/rapport")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadRapport([FromRoute] long id, [FromForm] UploadRapportDto request, CancellationToken ct)
        {
            var organisation = await GetCurrentOrganisationAsync(ct);
            if (organisation == null)
                return Unauthorized("Organisation introuvable pour ce token.");

            try
            {
                await _signalementManagementService.UploadRapportAsync(id, organisation.Id, request.Rapport, ct);
                return Ok(new { message = "Rapport PDF enregistre avec succes." });
            }
            catch (ArgumentException ex) { return BadRequest(ex.Message); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }
    }
}
