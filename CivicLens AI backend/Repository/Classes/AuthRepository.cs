using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicLens.API.Data;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CivicLens.API.Repository.Classes
{
    /// <summary>
    /// Repository d'authentification — accès direct à la base de données.
    /// 
    /// C'est l'équivalent de UserRepository dans le projet de référence.
    /// Même logique : BCrypt pour les mots de passe, SecurityTokenDescriptor pour JWT.
    /// 
   
    /// </summary>
    public class AuthRepository : Interfaces.IAuthRepository
    {
        private readonly AppDbContext _context;

        // Injection du DbContext (comme dans le projet de référence)
        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

      

        /// <summary>
        /// Cherche un utilisateur par email.
        /// Équivalent de getByMatricule() dans le projet de référence.
        /// </summary>
        public User GetByEmail(string email)
        {
            return _context.Users.FirstOrDefault(u => u.Email == email)!;
        }

        public bool IsEmailExists(string email)
        {
            return _context.Users.Any(u => u.Email == email);
        }

        public bool IsCinExists(string cin)
        {
            return _context.Citoyens.Any(c => c.Cin == cin);
        }

        public bool IsMatriculeExists(string matricule)
        {
            // Identique à IsMatriculeExists() du projet de référence
            return _context.BureauxDeControle.Any(b => b.Matricule == matricule);
        }

        /// Authentifie un utilisateur par email et mot de passe.
        /// 
        /// IDENTIQUE au projet de référence (Authenticate dans UserRepository) :
        /// 1. Cherche le user par email (comme Matricule dans la référence)
        /// 2. Si trouvé → BCrypt.Verify(mot_de_passe, hash)
        /// 3. Si mot de passe correct → retourne le user
        /// 4. Sinon → lève UnauthorizedAccessException
        /// 
        /// Dans le projet de référence :
        ///   var user = _context.Magasiniers.FirstOrDefault(u => u.Matricule == matricule);
        ///   if (BCrypt.Net.BCrypt.Verify(password, user.Password)) return user;
        /// 
        /// Ici c'est pareil, mais avec Email au lieu de Matricule.
        /// </summary>
        public User Authenticate(string email, string motDePasse)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == email.ToUpper());

            if (user != null)
            {
                // Vérifier que le compte est actif
                if (user.StatutCompte != StatutCompte.ACTIF)
                {
                    throw new UnauthorizedAccessException("Ce compte est désactivé ou suspendu.");
                }

                // Vérifier le mot de passe avec BCrypt (identique au projet de référence)
                if (BCrypt.Net.BCrypt.Verify(motDePasse, user.MotDePasseHash))
                {
                    // Mettre à jour la date de dernier login
                    user.DernierLogin = DateTime.UtcNow;
                    Save();
                    return user; // Mot de passe correct → retourne le user
                }
                else
                {
                    throw new UnauthorizedAccessException("Mot de passe incorrect.");
                }
            }
            else
            {
                throw new UnauthorizedAccessException("Utilisateur non trouvé.");
            }
        }

        /// <summary>
        /// Inscription d'un citoyen.
        /// 
        /// IDENTIQUE à Register() dans le projet de référence :
        ///   string hashedPassword = BCrypt.Net.BCrypt.HashPassword(magasinier.Password);
        ///   var newMagasinier = new Magasinier { ... Password = hashedPassword };
        ///   _context.Magasiniers.Add(newMagasinier);
        /// 
        /// Ici on fait pareil avec Citoyen.
        /// </summary>
        public Citoyen RegisterCitoyen(Citoyen citoyen)
        {
            // ── Validation CIN ──
            if (string.IsNullOrWhiteSpace(citoyen.Cin))
                throw new ArgumentException("Le CIN est obligatoire.");

            if (citoyen.Cin.Length != 8 || !citoyen.Cin.All(char.IsDigit))
                throw new ArgumentException("Le CIN doit contenir exactement 8 chiffres.");

            int cinValue = int.Parse(citoyen.Cin);
            if (cinValue < 1 || cinValue > 99999999)
                throw new ArgumentException("Le CIN doit être dans l'intervalle 00000001 et 99999999.");

            // ── Hash mot de passe ──
            citoyen.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(citoyen.MotDePasseHash);
            citoyen.Role = Role.CITOYEN;
            citoyen.StatutCompte = StatutCompte.ACTIF;

            _context.Citoyens.Add(citoyen);
            if (Save())
                return citoyen;

            return null!;
        }

        public User CreateAdmin(User admin)
        {
            admin.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(admin.MotDePasseHash);
            admin.Role = Role.ADMIN;
            admin.StatutCompte = StatutCompte.ACTIF;

            _context.Users.Add(admin);
            if (Save())
            {
                return admin;
            }
            return null!;
        }

        public BureauDeControle CreateBureau(BureauDeControle bureau)
        {
            bureau.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(bureau.MotDePasseHash);
            bureau.Role = Role.BUREAUDECONTROLE;
            bureau.StatutCompte = StatutCompte.ACTIF;

            _context.BureauxDeControle.Add(bureau);
            if (Save())
            {
                return bureau;
            }
            return null!;
        }

        public Organisation CreateOrganisation(Organisation organisation)
        {
            organisation.MotDePasseHash = BCrypt.Net.BCrypt.HashPassword(organisation.MotDePasseHash);
            organisation.Role = Role.ORGANISATION;
            organisation.StatutCompte = StatutCompte.ACTIF;

            _context.Organisations.Add(organisation);
            if (Save())
            {
                return organisation;
            }
            return null!;
        }

        /// <summary>
        /// Génère un token JWT.
        /// 
        /// C'EST EXACTEMENT le même code que GenerateToken() dans le projet de référence :
        /// 
        ///   var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        ///   var tokenDescriptor = new SecurityTokenDescriptor
        ///   {
        ///       Subject = new ClaimsIdentity(claims),
        ///       Expires = DateTime.UtcNow.AddMinutes(60),
        ///       SigningCredentials = new SigningCredentials(key, HmacSha256Signature)
        ///   };
        ///   var tokenHandler = new JwtSecurityTokenHandler();
        ///   var token = tokenHandler.CreateToken(tokenDescriptor);
        ///   return tokenHandler.WriteToken(token);
        /// 
        /// COMMENT ÇA MARCHE :
        /// 1. SymmetricSecurityKey → transforme la clé secrète (string) en clé cryptographique
        /// 2. SecurityTokenDescriptor → décrit le contenu du token :
        ///    - Subject : les claims (informations : email, rôle, id)
        ///    - Expires : quand le token expire (60 minutes)
        ///    - SigningCredentials : comment signer (HMAC-SHA256)
        /// 3. CreateToken → crée l'objet token
        /// 4. WriteToken → sérialise en string "eyJhbG..."
        /// </summary>
        public string GenerateToken(string secret, List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(60),
                SigningCredentials = new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Sauvegarde les changements en base de données.
        /// Identique à Save() dans le projet de référence :
        ///   var saved = _context.SaveChanges();
        ///   return saved > 0 ? true : false;
        /// </summary>
        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0;
        }
    }
}
