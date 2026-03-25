using CivicLens.API.Data;
using CivicLens.API.Models.DTOs;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CivicLens.API.Controllers
{
    /// <summary>
    /// Contrôleur d'authentification — endpoints publics.
    /// </summary>
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

        // ──────────────────────────────────────────────────────
        // POST /api/auth/login
        // ──────────────────────────────────────────────────────
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
                        new Claim(ClaimTypes.Role, user.Role.ToString())
                    };

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

        // ──────────────────────────────────────────────────────
        // POST /api/auth/register
        // ──────────────────────────────────────────────────────
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
                Adresse        = dto.Adresse,
                Nom            = dto.Nom,
                Prenom         = dto.Prenom,
                Cin            = dto.Cin
            };

            var registeredUser = _authService.RegisterCitoyen(citoyen);

            if (registeredUser != null)
                return Ok("Citoyen enregistré avec succès.");

            return BadRequest("Échec de l'inscription.");
        }

        // ──────────────────────────────────────────────────────
        // GET /api/auth/Reactivate?token=...
        // Public endpoint — user is locked out so no [Authorize]
        // ──────────────────────────────────────────────────────
        /// <summary>
        /// Validates a dedicated reactivation JWT and sets the Citoyen's
        /// StatutCompte back to ACTIF.
        /// The token must contain a "purpose": "reactivate" claim so that
        /// normal login tokens cannot be used here.
        /// </summary>
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

            // Verify it is a reactivation token, not a login token
            var purpose = principal.FindFirstValue("purpose");
            if (purpose != "reactivate")
                return BadRequest("Ce lien n'est pas un lien de réactivation valide.");

            var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId))
                return BadRequest("Identifiant utilisateur invalide.");

            var citoyen = await _db.Citoyens.FindAsync(userId);
            if (citoyen == null)
                return NotFound("Utilisateur introuvable.");

            if (citoyen.StatutCompte == StatutCompte.ACTIF)
                return Ok("Votre compte est déjà actif. Vous pouvez vous connecter normalement.");

            // Reactivate from INACTIF or SUSPENDU — the token was legitimately issued
            // when the user went INACTIF, but the background service may have since
            // pushed them to SUSPENDU before they clicked the link. Both cases are valid.
            citoyen.StatutCompte = StatutCompte.ACTIF;
            await _db.SaveChangesAsync();

            return Ok("Votre compte a été réactivé avec succès. Vous pouvez maintenant vous connecter.");
        }
    }
}
