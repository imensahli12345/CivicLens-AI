using CivicLens.API.Models.Entities;

public interface IBureauDeControleService
{
    BureauDeControle? GetBureauById(Guid id);
    Task<BureauDeControle?> GetBureauByIdAsync(Guid id, CancellationToken ct);
    Task<BureauDeControle?> GetFirstBureauAsync(CancellationToken ct);
    Task<List<Service>> GetServicesAsync(Guid bureauId, CancellationToken ct);
    //activate and desactivate service statut
    Task<Service> ActivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct);
    Task<Service> DeactivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct);
    Task<Service> UpdateServicePasswordAsync(Guid bureauId, Guid serviceId, string nouveauMotDePasse, CancellationToken ct);
    Service CreateService(Service service);
}
