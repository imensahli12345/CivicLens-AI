using CivicLens.API.Models.Entities;
using CivicLens.API.Data;
using CivicLens.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

public class BureauDeControleRepository : IBureauDeControleRepository
{
    private readonly AppDbContext _context;

    public BureauDeControleRepository(AppDbContext context)
    {
        _context = context;
    }

    public BureauDeControle? GetById(Guid id)
    {
        return _context.BureauxDeControle
                       .FirstOrDefault(b => b.Id == id);
    }

    public async Task<BureauDeControle?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.BureauxDeControle
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<BureauDeControle?> GetFirstAsync(CancellationToken ct)
    {
        return await _context.BureauxDeControle
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Service>> GetServicesAsync(Guid bureauId, CancellationToken ct)
    {
        return await _context.Services
            .AsNoTracking()
            .Where(s => s.BureauDeControleId == bureauId)
            .OrderBy(s => s.Domaine)
            .ToListAsync(ct);
    }

    public async Task<Service?> GetServiceByIdAsync(Guid bureauId, Guid serviceId, CancellationToken ct)
    {
        return await _context.Services
            .FirstOrDefaultAsync(s => s.Id == serviceId && s.BureauDeControleId == bureauId, ct);
    }

    public Service CreateService(Service service)
    {
        service.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(service.MotDePasseHash);
        service.Role = Role.SERVICE;
        service.StatutCompte = StatutCompte.INACTIF;

        _context.Services.Add(service);
        if (Save())
        {
            return service;
        }
        return null!;
    }

    public bool Save()
    {
        var saved = _context.SaveChanges();
        return saved > 0;
    }

    public async Task<bool> SaveAsync(CancellationToken ct)
    {
        return await _context.SaveChangesAsync(ct) > 0;
    }

    public async Task<Service> ActivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct)
    {
        var service = await GetServiceByIdAsync(bureauId, serviceId, ct)
            ?? throw new KeyNotFoundException("Service introuvable pour ce bureau.");

        if (service.StatutCompte != StatutCompte.ACTIF)
        {
            service.StatutCompte = StatutCompte.ACTIF;
            _context.Services.Update(service);
            await SaveAsync(ct);
        }
        return service;
    }

    public async Task<Service> DeactivateServiceAsync(Guid bureauId, Guid serviceId, CancellationToken ct)
    {
        var service = await GetServiceByIdAsync(bureauId, serviceId, ct)
            ?? throw new KeyNotFoundException("Service introuvable pour ce bureau.");

        if (service.StatutCompte != StatutCompte.INACTIF)
        {
            service.StatutCompte = StatutCompte.INACTIF;
            _context.Services.Update(service);
            await SaveAsync(ct);
        }
        return service;
    }
}
