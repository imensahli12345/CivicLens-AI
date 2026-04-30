using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

public class BureauDeControleService : IBureauDeControleService
{
    private readonly IBureauDeControleRepository _repository;

    public BureauDeControleService(IBureauDeControleRepository repository)
    {
        _repository = repository;
    }

    public BureauDeControle? GetBureauById(Guid id)
    {
        return _repository.GetById(id);
    }

    public async Task<BureauDeControle?> GetBureauByIdAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<BureauDeControle?> GetFirstBureauAsync(CancellationToken ct)
    {
        return await _repository.GetFirstAsync(ct);
    }

    public async Task<List<Service>> GetServicesAsync(Guid bureauId, CancellationToken ct)
    {
        return await _repository.GetServicesAsync(bureauId, ct);
    }

    public async Task<Service> ActivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct)
    {
        return await _repository.ActivateServiceAsync(bureauId, serviceId, ct);
    }

    public async Task<Service> DeactivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct)
    {
        return await _repository.DeactivateServiceAsync(bureauId, serviceId, ct);
    }

    public Service CreateService(Service service)
    {
        return _repository.CreateService(service);
    }
}
