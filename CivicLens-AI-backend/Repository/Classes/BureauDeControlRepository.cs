using CivicLens.API.Models.Entities;
using CivicLens.API.Data;
using CivicLens.API.Models.Enums;

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
}
