using CivicLens.API.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace CivicLens.API.Services.Classes
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendReactivationEmailAsync(string toEmail, string toName, string reactivationLink)
        {
            try
            {
                var host        = _config["SmtpSettings:Host"]!;
                var port        = int.Parse(_config["SmtpSettings:Port"]!);
                var username    = _config["SmtpSettings:Username"]!;
                var password    = _config["SmtpSettings:Password"]!;
                var senderEmail = _config["SmtpSettings:SenderEmail"]!;
                var senderName  = _config["SmtpSettings:SenderName"] ?? "CivicLens AI";

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl            = true,
                    Credentials          = new NetworkCredential(username, password),
                    DeliveryMethod       = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var body = $"""
                    <!DOCTYPE html>
                    <html lang="fr">
                    <head><meta charset="UTF-8"/></head>
                    <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;">
                      <div style="max-width:560px;margin:auto;background:#fff;border-radius:8px;padding:32px;box-shadow:0 2px 8px rgba(0,0,0,.1)">
                        <h2 style="color:#2563eb;">🏙️ CivicLens AI</h2>
                        <p>Bonjour <strong>{toName}</strong>,</p>
                        <p>Votre compte a été marqué comme <strong>inactif</strong> car vous ne vous êtes pas connecté(e) depuis un certain temps.</p>
                        <p>Pour réactiver votre compte, cliquez sur le bouton ci-dessous :</p>
                        <p style="text-align:center;margin:32px 0;">
                          <a href="{reactivationLink}"
                             style="background:#2563eb;color:#fff;padding:14px 28px;border-radius:6px;text-decoration:none;font-size:16px;">
                            ✅ Réactiver mon compte
                          </a>
                        </p>
                        <p style="color:#888;font-size:13px;">Ce lien expire dans 7 jours. Si vous n'avez pas demandé cette action, ignorez ce message.</p>
                        <hr style="border:none;border-top:1px solid #eee;margin:24px 0;"/>
                        <p style="color:#aaa;font-size:12px;text-align:center;">CivicLens AI — Plateforme de signalement citoyen</p>
                      </div>
                    </body>
                    </html>
                    """;

                var mail = new MailMessage
                {
                    From       = new MailAddress(senderEmail, senderName),
                    Subject    = "⚠️ Votre compte CivicLens est inactif – Réactivez-le",
                    Body       = body,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(toEmail, toName));

                await client.SendMailAsync(mail);
                _logger.LogInformation("[EmailService] Reactivation email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to send email to {Email}", toEmail);
                // We do NOT rethrow — a failed email should not crash the background service.
            }
        }
    }
}
