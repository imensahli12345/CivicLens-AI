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

        public AuthController(IAuthService authService, IConfiguration config, AppDbContext db)
        {
            _authService = authService;
            _config      = config;
            _db          = db;
        }

        [HttpPost]
        [Route("login")]
        public IActionResult Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var user = _authService.Authenticate(loginDto.Email, loginDto.MotDePasse);

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

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            if (citoyen.StatutCompte == StatutCompte.ACTIF)
                return Ok("Votre compte est déjà actif. Vous pouvez vous connecter normalement.");

            citoyen.StatutCompte = StatutCompte.ACTIF;
            await _db.SaveChangesAsync();

            return Ok("Votre compte a été réactivé avec succès. Vous pouvez maintenant vous connecter.");
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

            var userIdStr = User.FindFirstValue("nameid");
            if (!Guid.TryParse(userIdStr, out var userId))
                return Unauthorized("Token invalide.");

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            citoyen.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(dto.NouveauMotDePasse);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Mot de passe mis à jour avec succès." });
        }
    }
}
