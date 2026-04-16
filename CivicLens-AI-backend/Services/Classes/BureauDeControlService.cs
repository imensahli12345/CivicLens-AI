using CivicLens.API.Models.Entities;

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

    public Service CreateService(Service service)
    {
        return _repository.CreateService(service);
    }
}
