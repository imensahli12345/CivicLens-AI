using System.Security.Claims;
using CivicLens.API.Models.Entities;
using CivicLens.API.Repository.Interfaces;

namespace CivicLens.API.Services.Classes
{
    /// <summary>
    /// Service d'authentification — couche logique métier.
    /// 
    /// C'EST EXACTEMENT le même pattern que UserService dans le projet de référence :
    /// 
    ///   public class UserService : IUserService
    ///   {
    ///       private readonly IUserRepository _userrepository;
    ///       public UserService(IUserRepository userrepository) { _userrepository = userrepository; }
    ///       
    ///       public Magasinier Authenticate(string matricule, string password)
    ///       {
    ///           return _userrepository.Authenticate(matricule, password);
    ///       }
    ///       
    ///       public string GenerateToken(string secret, List&lt;Claim&gt; claims)
    ///       {
    ///           return _userrepository.GenerateToken(secret, claims);
    ///       }
    ///   }
    /// 
    /// Le Service délègue au Repository. C'est un intermédiaire.
    /// Pourquoi ? → Séparation des responsabilités :
    ///   - Le Controller gère les requêtes HTTP
    ///   - Le Service contient la logique métier
    ///   - Le Repository accède à la base de données
    /// </summary>
    public class AuthService : Interfaces.IAuthService
    {
        private readonly IAuthRepository _authRepository;

        // Injection de dépendances (identique au projet de référence)
        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository;
        }

        // ── Délègue au Repository (même pattern que UserService) ──

        public User Authenticate(string email, string motDePasse)
        {
            return _authRepository.Authenticate(email, motDePasse);
        }

        public string GenerateToken(string secret, List<Claim> claims)
        {
            return _authRepository.GenerateToken(secret, claims);
        }

        public bool IsEmailExists(string email)
        {
            return _authRepository.IsEmailExists(email);
        }

        public bool IsCinExists(string cin)
        {
            return _authRepository.IsCinExists(cin);
        }

        public bool IsMatriculeExists(string matricule)
        {
            return _authRepository.IsMatriculeExists(matricule);
        }

        public Citoyen RegisterCitoyen(Citoyen citoyen)
        {
            return _authRepository.RegisterCitoyen(citoyen);
        }

        public User CreateAdmin(User admin)
        {
            return _authRepository.CreateAdmin(admin);
        }

        public BureauDeControle CreateBureau(BureauDeControle bureau)
        {
            return _authRepository.CreateBureau(bureau);
        }

        public Organisation CreateOrganisation(Organisation organisation)
        {
            return _authRepository.CreateOrganisation(organisation);
        }
    }
}
