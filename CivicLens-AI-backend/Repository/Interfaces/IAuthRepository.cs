using System.Security.Claims;
using CivicLens.API.Models.Entities;

namespace CivicLens.API.Repository.Interfaces
{
    /// <summary>
    /// Interface du Repository d'authentification.
    /// Le Repository est la couche qui accède directement à la base de données.
    /// 
    /// ARCHITECTURE 3 COUCHES :
    /// ────────────────────────
    ///   Controller  →  Service  →  Repository  →  Base de données
    ///   (HTTP)         (Logique)    (Accès DB)      (PostgreSQL)
    /// 
    /// Le Repository contient :
    /// - Les requêtes LINQ vers la base de données
    /// - Le hashing/vérification des mots de passe (BCrypt)
    /// - La génération du token JWT
    /// </summary>
    public interface IAuthRepository
    {
        // ── Recherche ──
        User GetByEmail(string email);
        bool IsEmailExists(string email);
        bool IsCinExists(string cin);
        bool IsMatriculeExists(string matricule);

        // ── Authentification ──
        /// <summary>
        /// Vérifie le mot de passe avec BCrypt.
        /// Retourne le User si valide, sinon lève une exception.
        /// </summary>
        User Authenticate(string email, string motDePasse);

        // ── Enregistrement / Création ──
        Citoyen RegisterCitoyen(Citoyen citoyen);
        User CreateAdmin(User admin);
        BureauDeControle CreateBureau(BureauDeControle bureau);
        Organisation CreateOrganisation(Organisation organisation);
        

        // ── JWT ──
        /// <summary>
        /// Génère un token JWT signé avec la clé secrète.
        /// Identique à la méthode GenerateToken du projet de référence.
        /// </summary>
        string GenerateToken(string secret, List<Claim> claims);

        // ── Utilitaire ──
        bool Save();
    }
}
