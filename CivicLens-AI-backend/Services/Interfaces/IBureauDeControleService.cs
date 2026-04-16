using CivicLens.API.Models.Entities;

public interface IBureauDeControleService
{
    BureauDeControle? GetBureauById(Guid id);
    Service CreateService(Service service);
}
