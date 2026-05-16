namespace CivicLens.API.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendReactivationEmailAsync(string toEmail, string toName, string reactivationLink);
        Task SendActivationRequestEmailAsync(string toEmail, string toName, string requestedIdentifier, string requestedRole, string statut, string activationLink);
        Task SendReclamationEmailAsync(string fromEmail, string subject, string message, string? appVersion, string? context);
    }
}
