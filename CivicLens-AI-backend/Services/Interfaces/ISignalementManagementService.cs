using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using Microsoft.AspNetCore.Http;

namespace CivicLens.API.Services.Interfaces
{
    public interface ISignalementManagementService
    {
        Task<List<SignalementDto>> GetSignalementsAsync(Domaine? domaine, string delegation, SignalementFilterDto filter, CancellationToken ct);
        Task<List<SignalementDto>> GetAllSignalementsAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<List<SignalementDto>> GetOrganisationSignalementsAsync(Guid organisationId, SignalementFilterDto filter, CancellationToken ct);
        Task<List<SignalementDto>> GetAllOrganisationSignalementsAsync(Guid organisationId, CancellationToken ct);
        Task<SignalementDto?> GetSignalementByIdAsync(long id, Domaine? domaine, string delegation, CancellationToken ct);
        Task<SignalementDto?> GetOrganisationSignalementByIdAsync(long signalementId, Guid organisationId, CancellationToken ct);
        Task<List<HistoriqueStatut>> GetHistoriquesAsync(long signalementId, CancellationToken ct);
        Task<Organisation?> GetOrganisationByIdAsync(Guid id, CancellationToken ct);
        Task<List<Organisation>> GetOrganisationsAsync(Domaine? domaine, CancellationToken ct);
        Task<SignalementStatsDto> GetStatsAsync(Domaine? domaine, string delegation, CancellationToken ct);
        Task<Signalement> OuvrirSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, CancellationToken ct);
        Task<Signalement> EnvoyerSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, Guid organisationId, string? descriptionSupp, CancellationToken ct);
        Task<Signalement> RejeterSignalementAsync(long signalementId, Domaine? domaine, string delegation, Guid actorId, CancellationToken ct);
        Task<Signalement> CommencerEtudeOrganisationAsync(long signalementId, Guid organisationId, Guid actorId, CancellationToken ct);
        Task<Signalement> ResoudreSignalementOrganisationAsync(long signalementId, Guid organisationId, Guid actorId, CancellationToken ct);
        Task<string> UploadRapportAsync(long signalementId, Guid organisationId, IFormFile rapport, CancellationToken ct);
        Task<string?> GetCinByCitoyenIdAsync(Guid citoyenId, CancellationToken ct);
    }
}
