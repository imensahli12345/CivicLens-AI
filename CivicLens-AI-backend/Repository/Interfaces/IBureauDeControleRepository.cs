using CivicLens.API.Models.Entities;

public interface IBureauDeControleRepository
{
    BureauDeControle? GetById(Guid id);       // Get a bureau by its ID
    Service CreateService(Service service);   // Create a service linked to a bureau
}
