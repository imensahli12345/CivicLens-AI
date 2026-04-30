using CivicLens.API.Data;
using CivicLens.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class ServiceAccountRepository : IServiceAccountRepository
{
    private readonly AppDbContext _context;

    public ServiceAccountRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Service?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<Service?> GetFirstAsync(CancellationToken ct)
    {
        return await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }
}
