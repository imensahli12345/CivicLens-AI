using CivicLens.API.Models.DTOs;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CivicLens.API.Models.Entities;

namespace CivicLens.API.Controllers
{
    [Route("api/service")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly IServiceAccountService _serviceAccountService;
        private readonly ISignalementManagementService _signalementManagementService;
        private readonly IHistoriqueService _historiqueService;
        private readonly IOrganisationService _organisationService;
        private readonly IWebHostEnvironment _env;

        public ServiceController(
            IServiceAccountService serviceAccountService,
            ISignalementManagementService signalementManagementService,
            IHistoriqueService historiqueService,
            IOrganisationService organisationService,
            IWebHostEnvironment env)
        {
            _serviceAccountService = serviceAccountService;
            _signalementManagementService = signalementManagementService;
            _historiqueService = historiqueService;
            _organisationService = organisationService;
            _env = env;
        }

        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirst("nameid")?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }

        private async Task<CivicLens.API.Models.Entities.Service?> GetCurrentServiceAsync(CancellationToken ct)
        {
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                if (!_env.IsDevelopment())
                    return null;

                return await _serviceAccountService.GetFirstAsync(ct);
            }

            return await _serviceAccountService.GetByIdAsync(userId.Value, ct);
        }

        // =========================
        // GET ALL SIGNALMENTS
        // =========================
        [HttpGet("signalements/tous")]
        public async Task<IActionResult> GetAllSignalements(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .GetAllSignalementsAsync(service.Domaine, service.Delegation, ct);

            return Ok(result);
        }

        // =========================
        // FILTER SIGNALMENTS
        // =========================
        [HttpGet("signalements")]
        public async Task<IActionResult> GetSignalements([FromQuery] SignalementFilterDto filter, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            try
            {
                var result = await _signalementManagementService
                    .GetSignalementsAsync(service.Domaine, service.Delegation, filter, ct);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =========================
        // GET BY ID
        // =========================
        [HttpGet("signalements/{id:long}")]
        public async Task<IActionResult> GetSignalementById(long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .GetSignalementByIdAsync(id, service.Domaine, service.Delegation, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // =========================
        // HISTORIQUE
        // =========================
        [HttpGet("signalements/{id:long}/historique")]
        public async Task<IActionResult> GetHistorique(long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _historiqueService.GetExpandedBySignalementIdAsync(id, ct);
            return Ok(result);
        }

        // =========================
        // OUVRIR
        // =========================
        [HttpPut("signalements/{id:long}/ouvrir")]
        public async Task<IActionResult> Ouvrir(long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .OuvrirSignalementAsync(id, service.Domaine, service.Delegation, service.Id, ct);

            return Ok(result);
        }

        // =========================
        // ENVOYER
        // =========================
        [HttpPut("signalements/{id:long}/envoyer")]
        public async Task<IActionResult> Envoyer(long id, [FromBody] SendSignalementDto dto, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .EnvoyerSignalementAsync(
                    id,
                    service.Domaine,
                    service.Delegation,
                    service.Id,
                    dto.OrganisationId,
                    dto.DescriptionSupplementaire,
                    ct);

            return Ok(result);
        }

        // =========================
        // REJETER
        // =========================
        [HttpPut("signalements/{id:long}/rejeter")]
        public async Task<IActionResult> Rejeter(long id, CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .RejeterSignalementAsync(id, service.Domaine, service.Delegation, service.Id, ct);

            return Ok(result);
        }

        // =========================
        // STATS
        // =========================
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _signalementManagementService
                .GetStatsAsync(service.Domaine, service.Delegation, ct);

            return Ok(result);
        }

        // =========================
        // ORGANISATIONS
        // =========================
      //*  [HttpGet("organisations")]
        /*public async Task<IActionResult> GetOrganisations(CancellationToken ct)
        {
            var service = await GetCurrentServiceAsync(ct);
            if (service == null)
                return Unauthorized();

            var result = await _organisationService
                .GetListAsync(service.Domaine, ct);

            return Ok(result);
        }*/
    }
}