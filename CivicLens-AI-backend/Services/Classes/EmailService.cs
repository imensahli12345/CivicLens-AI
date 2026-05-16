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

        public async Task SendReclamationEmailAsync(string fromEmail, string subject, string message, string? appVersion, string? context)
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
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Réclamation" : subject.Trim();
                var safeFrom = string.IsNullOrWhiteSpace(fromEmail) ? "unknown" : fromEmail.Trim();
                var safeContext = string.IsNullOrWhiteSpace(context) ? "unknown" : context.Trim();
                var safeVersion = string.IsNullOrWhiteSpace(appVersion) ? "unknown" : appVersion.Trim();

                var body = $"""
                    <!DOCTYPE html>
                    <html lang="fr">
                    <head><meta charset="UTF-8"/></head>
                    <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;">
                      <div style="max-width:680px;margin:auto;background:#fff;border-radius:8px;padding:24px;box-shadow:0 2px 8px rgba(0,0,0,.1)">
                        <h2 style="margin:0 0 8px 0;color:#0f172a;">Réclamation — CivicLens AI</h2>
                        <p style="margin:0 0 16px 0;color:#475569;">Signalement envoyé depuis l'application.</p>
                        <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:12px 14px;margin-bottom:16px;">
                          <div style="font-size:13px;color:#334155;"><strong>De :</strong> {WebUtility.HtmlEncode(safeFrom)}</div>
                          <div style="font-size:13px;color:#334155;"><strong>Contexte :</strong> {WebUtility.HtmlEncode(safeContext)}</div>
                          <div style="font-size:13px;color:#334155;"><strong>Version :</strong> {WebUtility.HtmlEncode(safeVersion)}</div>
                        </div>
                        <div style="white-space:pre-wrap;color:#0f172a;border:1px solid #e2e8f0;border-radius:8px;padding:14px;">
                          {WebUtility.HtmlEncode((message ?? string.Empty).Trim())}
                        </div>
                      </div>
                    </body>
                    </html>
                    """;

                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"📩 Réclamation CivicLens — {safeSubject}",
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(senderEmail, "Support CivicLens"));
                mail.ReplyToList.Add(new MailAddress(safeFrom));

                await client.SendMailAsync(mail);
                _logger.LogInformation("[EmailService] Reclamation email sent (from={From}).", safeFrom);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to send reclamation email.");
            }
        }

        public async Task SendActivationRequestEmailAsync(string toEmail, string toName, string requestedIdentifier, string requestedRole, string statut, string activationLink)
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
                    EnableSsl = true,
                    Credentials = new NetworkCredential(username, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var safeToName = string.IsNullOrWhiteSpace(toName) ? toEmail : toName.Trim();
                var safeIdentifier = string.IsNullOrWhiteSpace(requestedIdentifier) ? "—" : requestedIdentifier.Trim();
                var safeRole = string.IsNullOrWhiteSpace(requestedRole) ? "—" : requestedRole.Trim();
                var safeStatus = string.IsNullOrWhiteSpace(statut) ? "—" : statut.Trim();

                var body = $"""
                    <!DOCTYPE html>
                    <html lang="fr">
                    <head><meta charset="UTF-8"/></head>
                    <body style="font-family:Arial,sans-serif;background:#f4f4f4;padding:20px;">
                      <div style="max-width:680px;margin:auto;background:#fff;border-radius:10px;padding:24px;box-shadow:0 2px 8px rgba(0,0,0,.08)">
                        <h2 style="margin:0 0 8px 0;color:#0f172a;">Demande d’activation — CivicLens AI</h2>
                        <p style="margin:0 0 16px 0;color:#475569;">Une demande d’activation a été envoyée depuis la page de connexion.</p>

                        <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:12px 14px;margin-bottom:16px;">
                          <div style="font-size:13px;color:#334155;"><strong>Compte :</strong> {WebUtility.HtmlEncode(safeIdentifier)}</div>
                          <div style="font-size:13px;color:#334155;"><strong>Rôle :</strong> {WebUtility.HtmlEncode(safeRole)}</div>
                          <div style="font-size:13px;color:#334155;"><strong>Statut :</strong> {WebUtility.HtmlEncode(safeStatus)}</div>
                        </div>

                        <p style="margin:0 0 14px 0;color:#334155;">
                          Bonjour <strong>{WebUtility.HtmlEncode(safeToName)}</strong>,<br/>
                          Si vous confirmez cette demande, cliquez sur le bouton ci-dessous pour activer le compte.
                        </p>

                        <p style="text-align:center;margin:18px 0 10px;">
                          <a href="{activationLink}"
                             style="background:#2563eb;color:#fff;padding:12px 18px;border-radius:8px;text-decoration:none;font-size:15px;font-weight:700;display:inline-block;">
                            Activer le compte
                          </a>
                        </p>

                        <p style="margin:0;color:#64748b;font-size:12px;">
                          Ce lien expire dans 7 jours.
                        </p>
                      </div>
                    </body>
                    </html>
                    """;

                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "🔐 Demande d’activation — CivicLens AI",
                    Body = body,
                    IsBodyHtml = true
                };
                mail.To.Add(new MailAddress(toEmail, safeToName));

                await client.SendMailAsync(mail);
                _logger.LogInformation("[EmailService] Activation request email sent to {Email}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Failed to send activation request email.");
            }
        }
    }
}
