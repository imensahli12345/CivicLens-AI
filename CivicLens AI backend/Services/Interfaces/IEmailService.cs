namespace CivicLens.API.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendReactivationEmailAsync(string toEmail, string toName, string reactivationLink);
    }
}
