using CivicLens.API.Data;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CivicLens.API.Services.Classes
{
    public class InactivityCheckService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;//this is singleton and her role is 
        private readonly IConfiguration _config;
        private readonly ILogger<InactivityCheckService> _logger;// the role of the logger is to log the errors and the information

        // ── Timing constants
        // PRODUCTION: CheckInterval=24h  | InactifThreshold=3 months | SuspenduExtra=1 month
        // SuspenduCutoff = now - (InactifThreshold + SuspenduExtra)
        // → A user becomes SUSPENDU if they have been inactive for InactifThreshold AND
        //   still haven't reactivated after SuspenduExtra more time.
        private static readonly TimeSpan CheckInterval    = TimeSpan.FromHours(24);//every 24 hours
        private static readonly TimeSpan InactifThreshold = TimeSpan.FromDays(90);     // → INACTIF after ~3 months
        private static readonly TimeSpan SuspenduExtra    = TimeSpan.FromDays(30);     // → SUSPENDU ~1 month after INACTIF

        public InactivityCheckService(
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<InactivityCheckService> logger)
        {
            _scopeFactory = scopeFactory;
            _config       = config;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[InactivityCheckService] Service started. Checking every {Interval}.", CheckInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("[InactivityCheckService] Running inactivity check at {Time}", DateTime.UtcNow);

                try
                {
                    await RunChecksAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[InactivityCheckService] Error during inactivity check.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task RunChecksAsync(CancellationToken ct)
        {
            // BackgroundService is Singleton → must create a scope to resolve Scoped services.
            using var scope  = _scopeFactory.CreateScope();
            var db           = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now            = DateTime.UtcNow;
            var inactifCutoff  = now - InactifThreshold;                      // 5 min ago
            var suspenduCutoff = now - (InactifThreshold + SuspenduExtra);    // 6 min ago
            var baseUrl        = _config["AppSettings:BaseUrl"]!.TrimEnd('/');

            // ════════════════════════════════════════════════════════
            // RULE 2 first — INACTIF → SUSPENDU
            // Uses ExecuteUpdateAsync → direct SQL UPDATE, no change tracking needed.
            // ════════════════════════════════════════════════════════
            var toSuspend = await db.Citoyens
                .Where(c => c.StatutCompte == StatutCompte.INACTIF
                         && (c.DernierLogin != null
                                 ? c.DernierLogin < suspenduCutoff
                                 : c.DateCreation < suspenduCutoff))
                .ToListAsync(ct);

            if (toSuspend.Count > 0)
            {
                var suspendIds = toSuspend.Select(c => c.Id).ToList();

                // Direct SQL UPDATE on Users table — bypasses EF TPT change-tracking issue
                await db.Users
                    .Where(u => suspendIds.Contains(u.Id))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(u => u.StatutCompte, StatutCompte.SUSPENDU), ct);

                foreach (var c in toSuspend)
                    _logger.LogInformation(
                        "[InactivityCheckService] User {Id} ({Email}) → SUSPENDU", c.Id, c.Email);
            }

            // ════════════════════════════════════════════════════════
            // RULE 1 — ACTIF → INACTIF + send reactivation email
            // ════════════════════════════════════════════════════════
            var toDeactivate = await db.Citoyens
                .Where(c => c.StatutCompte == StatutCompte.ACTIF
                         && (c.DernierLogin != null
                                 ? c.DernierLogin < inactifCutoff
                                 : c.DateCreation < inactifCutoff))
                .ToListAsync(ct);

            if (toDeactivate.Count > 0)
            {
                var deactivateIds = toDeactivate.Select(c => c.Id).ToList();

                // Direct SQL UPDATE on Users table
                await db.Users
                    .Where(u => deactivateIds.Contains(u.Id))
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(u => u.StatutCompte, StatutCompte.INACTIF), ct);

                foreach (var citoyen in toDeactivate)
                {
                    _logger.LogInformation(
                        "[InactivityCheckService] User {Id} ({Email}) → INACTIF", citoyen.Id, citoyen.Email);

                    var reactivationToken = GenerateReactivationToken(citoyen.Id, citoyen.Email);
                    var reactivationLink  = $"{baseUrl}/api/Auth/Reactivate?token={reactivationToken}";

                    var fullName = $"{citoyen.Prenom} {citoyen.Nom}".Trim();
                    if (string.IsNullOrEmpty(fullName)) fullName = citoyen.Email;

                    await emailService.SendReactivationEmailAsync(citoyen.Email, fullName, reactivationLink);
                }
            }

            if (toSuspend.Count == 0 && toDeactivate.Count == 0)
                _logger.LogInformation("[InactivityCheckService] No status changes needed.");
            else
                _logger.LogInformation(
                    "[InactivityCheckService] Done: {Suspended} suspended, {Deactivated} deactivated.",
                    toSuspend.Count, toDeactivate.Count);
        }

        /// <summary>
        /// Generates a short-lived JWT used exclusively for account reactivation.
        /// It contains a "purpose": "reactivate" claim so the endpoint can verify
        /// it's not a normal login token.
        /// </summary>
        private string GenerateReactivationToken(Guid userId, string email)
        {
            var key        = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds      = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("purpose", "reactivate")
            };

            var token = new JwtSecurityToken(
                claims:  claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
