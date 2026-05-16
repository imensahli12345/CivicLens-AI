using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
namespace CivicLens.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, IConfiguration config, AppDbContext db, IEmailService emailService)
        {
            _authService = authService;
            _config      = config;
            _db          = db;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var identifier = (loginDto.Email ?? string.Empty).Trim();
                if (identifier.Length == 8 && identifier.All(char.IsDigit))
                {
                    var citoyen = _db.Citoyens.FirstOrDefault(c => c.Cin == identifier);
                    if (citoyen == null)
                        return NotFound("Utilisateur non trouvé.");

                    if (citoyen.StatutCompte != StatutCompte.ACTIF)
                        return Unauthorized("Ce compte est désactivé ou suspendu.");

                    if (!BCrypt.Net.BCrypt.Verify(loginDto.MotDePasse, citoyen.MotDePasseHash))
                        return Unauthorized("Mot de passe incorrect.");

                    citoyen.DernierLogin = DateTime.UtcNow;
                    _db.SaveChanges();

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, citoyen.Id.ToString()),
                        new Claim(ClaimTypes.Email, citoyen.Email),
                        new Claim(ClaimTypes.Role, citoyen.Role.ToString()),
                        new Claim("region", citoyen.Region ?? ""),
                        new Claim("delegation", citoyen.Delegation ?? ""),
                        new Claim("nom", citoyen.Nom ?? ""),
                        new Claim("prenom", citoyen.Prenom ?? ""),
                        new Claim("cin", citoyen.Cin ?? ""),
                        new Claim("telephone", citoyen.Telephone ?? "")
                    };

                    var tokenByCin = _authService.GenerateToken(_config["Jwt:Key"]!, claims);
                    return Ok(tokenByCin);
                }

                var user = _authService.Authenticate(identifier, loginDto.MotDePasse);

                if (user != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Role.ToString()),
                        new Claim("region", user.Region ?? ""),
                        new Claim("delegation", user.Delegation ?? "")
                    };

                    if (user is Citoyen citoyen)
                    {
                        claims.Add(new Claim("nom", citoyen.Nom ?? ""));
                        claims.Add(new Claim("prenom", citoyen.Prenom ?? ""));
                        claims.Add(new Claim("cin", citoyen.Cin ?? ""));
                        claims.Add(new Claim("telephone", citoyen.Telephone ?? ""));
                    }

                    var token = _authService.GenerateToken(_config["Jwt:Key"]!, claims);
                    return Ok(token);
                }
                else
                {
                    return NotFound("Utilisateur non trouvé.");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                if (ex.Message == "Mot de passe incorrect.")
                    return Unauthorized("Mot de passe incorrect.");
                else if (ex.Message == "Ce compte est désactivé ou suspendu.")
                    return Unauthorized("Ce compte est désactivé ou suspendu.");
                else
                    return NotFound("Utilisateur non trouvé.");
            }
        }

        [HttpPost]
        [Route("register")]
        public IActionResult Register([FromBody] RegisterCitoyenDto dto)
        {
            if (_authService.IsEmailExists(dto.Email))
                return BadRequest("Cet email est déjà utilisé.");

            if (_authService.IsCinExists(dto.Cin))
                return BadRequest("Ce numéro CIN est déjà enregistré.");

            var citoyen = new Citoyen
            {
                Email          = dto.Email,
                MotDePasseHash = dto.MotDePasse,
                Region         = dto.Region,
                Delegation     = dto.Delegation,
                Nom            = dto.Nom,
                Prenom         = dto.Prenom,
                Cin            = dto.Cin
            };

            var registeredUser = _authService.RegisterCitoyen(citoyen);

            if (registeredUser != null)
                return Ok("Citoyen enregistré avec succès.");

            return BadRequest("Échec de l'inscription.");
        }

        [HttpGet("Reactivate")]
        public async Task<IActionResult> Reactivate([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return BadRequest("Token manquant.");

            var handler = new JwtSecurityTokenHandler();
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

            ClaimsPrincipal principal;
            try
            {
                principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateIssuerSigningKey  = true,
                    ValidateLifetime          = true,
                    IssuerSigningKey          = key
                }, out _);
            }
            catch (SecurityTokenExpiredException)
            {
                return Unauthorized("Ce lien a expiré. Veuillez vous reconnecter pour recevoir un nouveau lien.");
            }
            catch
            {
                return BadRequest("Token invalide ou corrompu.");
            }

            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "reactivate")
                return BadRequest("Ce lien n'est pas un lien de réactivation valide.");

            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("nameid");
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Identifiant utilisateur invalide.");

            var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("Utilisateur introuvable.");

            if (user.StatutCompte == StatutCompte.ACTIF)
                return Ok("Votre compte est déjà actif. Vous pouvez vous connecter normalement.");

            await _db.Users.Where(u => u.Id == userId)
                .ExecuteUpdateAsync(s => s.SetProperty(u => u.StatutCompte, StatutCompte.ACTIF));

            return Ok("Votre compte a été réactivé avec succès. Vous pouvez maintenant vous connecter.");
        }

        [HttpPost("reactivation-request")]
        [AllowAnonymous]
        public async Task<IActionResult> RequestReactivation([FromBody] ReactivationRequestDto dto, CancellationToken ct)
        {
            var identifier = (dto.Identifier ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(identifier))
                return BadRequest("Email ou CIN requis.");

            User? user = null;
            string requestedIdentifier = identifier;

            if (LooksLikeCin(identifier))
            {
                var cin = identifier;
                var citoyen = await _db.Citoyens.AsNoTracking().FirstOrDefaultAsync(c => c.Cin == cin, ct);
                if (citoyen != null)
                {
                    user = citoyen;
                    requestedIdentifier = $"{citoyen.Email} (CIN {citoyen.Cin})";
                }
            }
            else
            {
                var email = identifier.ToUpperInvariant();
                user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email.ToUpper() == email, ct);
                if (user is Citoyen citoyenUser && !string.IsNullOrWhiteSpace(citoyenUser.Cin))
                {
                    requestedIdentifier = $"{citoyenUser.Email} (CIN {citoyenUser.Cin})";
                }
            }

            if (user == null)
                return NotFound("Compte introuvable.");

            if (user.StatutCompte == StatutCompte.ACTIF)
                return BadRequest("Votre compte est déjà actif. Connectez-vous normalement.");

            if (user.Role == Role.CITOYEN)
            {
                await _db.Users
                    .Where(u => u.Id == user.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(u => u.StatutCompte, StatutCompte.ACTIF), ct);

                return Ok(new
                {
                    message = "Votre compte a été activé automatiquement. Vous pouvez vous connecter.",
                    statut = StatutCompte.ACTIF.ToString()
                });
            }

            var token = GenerateReactivationToken(user.Id, user.Email);
            var baseUrl = (_config["AppSettings:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}").TrimEnd('/');
            var link = $"{baseUrl}/api/Auth/Reactivate?token={token}";

            var requestedRole = user.Role.ToString();
            var statut = user.StatutCompte.ToString();

            if (user.Role == Role.SERVICE)
            {
                var service = await _db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == user.Id, ct);
                if (service == null)
                    return BadRequest("Service introuvable.");

                var bureau = await _db.BureauxDeControle.AsNoTracking().FirstOrDefaultAsync(b => b.Id == service.BureauDeControleId, ct);
                if (bureau == null)
                    return BadRequest("Bureau de contrôle introuvable.");

                requestedIdentifier = $"{user.Email} — {service.Nom} ({service.Domaine}) — {user.Delegation}";

                _db.Notifications.Add(new Notification
                {
                    CitoyenId = bureau.Id,
                    Type = "ACTIVATION_REQUEST_SERVICE",
                    RelatedUserId = user.Id,
                    ActionToken = token,
                    Message = $"Demande d’activation (SERVICE) : {requestedIdentifier} | Statut: {statut}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                var admins = await _db.Users.AsNoTracking()
                    .Where(u => u.Role == Role.ADMIN || u.Role == Role.SUPERADMIN)
                    .Select(u => u.Id)
                    .ToListAsync(ct);

                foreach (var adminId in admins)
                {
                    _db.Notifications.Add(new Notification
                    {
                        CitoyenId = adminId,
                        Type = "ACTIVATION_REQUEST",
                        RelatedUserId = user.Id,
                        ActionToken = token,
                        Message = $"Demande d’activation ({requestedRole}) : {requestedIdentifier} | Statut: {statut}",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Demande envoyée au responsable sur la plateforme.", statut = user.StatutCompte.ToString() });
        }

        private bool LooksLikeCin(string value)
        {
            return value.Length == 8 && value.All(char.IsDigit);
        }

        private string GenerateReactivationToken(Guid userId, string email)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim("purpose", "reactivate")
            };

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("me")]
        [Authorize(Roles = "CITOYEN")]
        public async Task<IActionResult> GetMe()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            return Ok(new
            {
                id = citoyen.Id,
                email = citoyen.Email,
                nom = citoyen.Nom,
                prenom = citoyen.Prenom,
                region = citoyen.Region,
                delegation = citoyen.Delegation,
                cin = citoyen.Cin,
                telephone = citoyen.Telephone,
                role = citoyen.Role.ToString(),
                photoUrl = citoyen.PhotoUrl
            });
        }

        [HttpPut("me")]
        [Authorize(Roles = "CITOYEN")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateCitoyenDto dto)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            citoyen.Nom = dto.Nom ?? citoyen.Nom;
            citoyen.Prenom = dto.Prenom ?? citoyen.Prenom;
            citoyen.Region = dto.Region ?? citoyen.Region;
            citoyen.Delegation = dto.Delegation ?? citoyen.Delegation;
            citoyen.Telephone = dto.Telephone ?? citoyen.Telephone;
            citoyen.PhotoUrl = dto.PhotoUrl ?? citoyen.PhotoUrl;

            await _db.SaveChangesAsync();

            return Ok(new { message = "Profil mis à jour avec succès" });
        }

        [HttpPut("me/password")]
        [Authorize(Roles = "CITOYEN")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NouveauMotDePasse))
                return BadRequest("Le nouveau mot de passe est requis.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid");
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            citoyen.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Mot de passe mis à jour avec succès." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(dto.Cin) || dto.Cin.Trim().Length != 8 || !dto.Cin.Trim().All(char.IsDigit))
                return BadRequest("CIN invalide.");

            var inputPhoneLocal8 = NormalizeTnLocalPhone8(dto.Telephone);
            if (string.IsNullOrWhiteSpace(inputPhoneLocal8))
                return BadRequest("Numéro de téléphone invalide.");

            if (string.IsNullOrWhiteSpace(dto.NouveauMotDePasse) || dto.NouveauMotDePasse.Trim().Length < 6)
                return BadRequest("Mot de passe invalide (min 6 caractères).");

            var cin = dto.Cin.Trim();

            var citoyen = await _db.Citoyens.FirstOrDefaultAsync(c => c.Cin == cin, ct);

            if (citoyen == null || citoyen.StatutCompte != StatutCompte.ACTIF)
                return BadRequest("Informations invalides.");

            var storedPhoneLocal8 = NormalizeTnLocalPhone8(citoyen.Telephone);
            if (string.IsNullOrWhiteSpace(storedPhoneLocal8) || storedPhoneLocal8 != inputPhoneLocal8)
                return BadRequest("Informations invalides.");

            citoyen.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse.Trim());
            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Mot de passe mis à jour avec succès." });
        }

        private static string NormalizePhoneDigits(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var digits = new string(value.Where(char.IsDigit).ToArray());
            return digits.Trim();
        }

        private static string NormalizeTnLocalPhone8(string? value)
        {
            var digits = NormalizePhoneDigits(value);
            if (string.IsNullOrWhiteSpace(digits)) return string.Empty;

            if (digits.Length == 8) return digits;

            if (digits.Length == 9 && digits.StartsWith("0")) return digits.Substring(1);

            if (digits.Length == 11 && digits.StartsWith("216")) return digits.Substring(3);

            if (digits.Length > 8) return digits.Substring(digits.Length - 8);

            return string.Empty;
        }

        [HttpDelete("me")]
        [Authorize(Roles = "CITOYEN")]
        public async Task<IActionResult> DeleteMe(CancellationToken ct)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid");
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var citoyen = await _db.Citoyens.FirstOrDefaultAsync(c => c.Id == userId, ct);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            var notifications = await _db.Notifications.Where(n => n.CitoyenId == userId).ToListAsync(ct);
            if (notifications.Count > 0) _db.Notifications.RemoveRange(notifications);

            var votes = await _db.Votes.Where(v => v.CitoyenId == userId).ToListAsync(ct);
            if (votes.Count > 0) _db.Votes.RemoveRange(votes);

            var participations = await _db.Participations.Where(p => p.CitoyenId == userId).ToListAsync(ct);
            if (participations.Count > 0) _db.Participations.RemoveRange(participations);

            citoyen.StatutCompte = StatutCompte.INACTIF;
            citoyen.Email = $"deleted-{citoyen.Id}@deleted.local";
            citoyen.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N") + DateTime.UtcNow.Ticks);
            citoyen.Nom = "Compte";
            citoyen.Prenom = "supprimé";
            citoyen.Cin = string.Empty;
            citoyen.Telephone = null;
            citoyen.PhotoUrl = null;
            citoyen.Region = string.Empty;
            citoyen.Delegation = string.Empty;

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return Ok(new { message = "Compte supprimé. Vous avez été déconnecté." });
        }

        [HttpPost("reclamation")]
        [AllowAnonymous]
        public async Task<IActionResult> SendReclamation([FromBody] ReclamationDto dto, CancellationToken ct)
        {
            var userIdStr = User?.Identity?.IsAuthenticated == true
                ? (User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("nameid"))
                : null;
            Guid? userId = null;
            if (!string.IsNullOrWhiteSpace(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
                userId = parsedUserId;

            var rawEmail =
                User?.Identity?.IsAuthenticated == true
                    ? (User.FindFirstValue(ClaimTypes.Email) ?? dto.Email)
                    : dto.Email;

            if (string.IsNullOrWhiteSpace(rawEmail))
                return BadRequest("Email requis.");

            if (string.IsNullOrWhiteSpace(dto.Message) || dto.Message.Trim().Length < 10)
                return BadRequest("Message requis (min 10 caractères).");

            var subject = string.IsNullOrWhiteSpace(dto.Subject) ? "Réclamation" : dto.Subject.Trim();
            var message = dto.Message.Trim();

            var reclamation = new PlatformReclamation
            {
                FromUserId = userId,
                FromEmail = rawEmail.Trim(),
                Subject = subject,
                Message = message,
                AppVersion = dto.AppVersion,
                Context = dto.Context,
                Status = "NOUVELLE"
            };

            _db.PlatformReclamations.Add(reclamation);

            var admins = await _db.Users.AsNoTracking()
                .Where(u => u.Role == Role.ADMIN || u.Role == Role.SUPERADMIN)
                .Select(u => u.Id)
                .ToListAsync(ct);

            foreach (var adminId in admins)
            {
                _db.Notifications.Add(new Notification
                {
                    CitoyenId = adminId,
                    Type = "PLATFORM_RECLAMATION",
                    RelatedReclamationId = reclamation.Id,
                    Message = $"Nouvelle réclamation : {subject} — {reclamation.FromEmail}",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);

            return Ok(new { message = "Réclamation envoyée sur la plateforme." });
        }

        public class ReclamationDto
        {
            public string Email { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? AppVersion { get; set; }
            public string? Context { get; set; }
        }

        public class ResetPasswordDto
        {
            public string Cin { get; set; } = string.Empty;
            public string Telephone { get; set; } = string.Empty;
            public string NouveauMotDePasse { get; set; } = string.Empty;
        }

        public class ReactivationRequestDto
        {
            public string Identifier { get; set; } = string.Empty;
        }
    }
}
