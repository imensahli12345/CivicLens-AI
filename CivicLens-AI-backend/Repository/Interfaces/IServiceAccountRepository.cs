using CivicLens.API.Models.Entities;

public interface IServiceAccountRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Service?> GetFirstAsync(CancellationToken ct);
}
