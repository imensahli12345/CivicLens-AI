using CivicLens.API.Models.Entities;

public class ServiceAccountService : IServiceAccountService
{
    private readonly IServiceAccountRepository _repository;

    public ServiceAccountService(IServiceAccountRepository repository)
    {
        _repository = repository;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _repository.GetByIdAsync(id, ct);
    }

    public async Task<Service?> GetFirstAsync(CancellationToken ct)
    {
        return await _repository.GetFirstAsync(ct);
    }
}
