using System.Security.Claims;
using CivicLens.API.Models.Entities;

namespace CivicLens.API.Services.Interfaces
{
    /// <summary>
    /// Interface du Service d'authentification.
    /// Le Service est la couche intermédiaire entre le Controller et le Repository.
    /// 
    /// ARCHITECTURE (identique au projet de référence) :
    /// ──────────────────────────────────────────────────
    ///   Controller  →  IAuthService  →  IAuthRepository  →  DbContext
    ///   (AuthController)  (AuthService)    (AuthRepository)     (AppDbContext)
    /// 
    /// Dans le projet de référence :
    ///   Controller → IUserService → IUserRepository → inventoryBDContext
    /// 
    /// C'est le même pattern, adapté à CivicLens.
    /// </summary>
    public interface IAuthService
    {
        // ── Authentification ──
        User Authenticate(string email, string motDePasse);
        string GenerateToken(string secret, List<Claim> claims);

        // ── Vérifications ──
        bool IsEmailExists(string email);
        bool IsCinExists(string cin);
        bool IsMatriculeExists(string matricule);

        // ── Création de comptes ──
        Citoyen RegisterCitoyen(Citoyen citoyen);
        User CreateAdmin(User admin);
        BureauDeControle CreateBureau(BureauDeControle bureau);
        Organisation CreateOrganisation(Organisation organisation);
    }
}
