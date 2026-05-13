using CivicLens.API.Models.Entities;

public interface IBureauDeControleRepository
{
    BureauDeControle? GetById(Guid id);       // Get a bureau by its ID
    Task<BureauDeControle?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<BureauDeControle?> GetFirstAsync(CancellationToken ct);
    Task<List<Service>> GetServicesAsync(Guid bureauId, CancellationToken ct);
    Task<Service?> GetServiceByIdAsync(Guid bureauId, Guid serviceId, CancellationToken ct);
    Service CreateService(Service service);   // Create a service linked to a bureau
    Task<bool> SaveAsync(CancellationToken ct);

    Task<Service> ActivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct);
    Task<Service> DeactivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct);
}
