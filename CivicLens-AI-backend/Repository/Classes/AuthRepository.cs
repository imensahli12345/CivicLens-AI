using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicLens.API.Data;
using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CivicLens.API.Repository.Classes
{
    public class AuthRepository : Interfaces.IAuthRepository
    {
        private readonly AppDbContext _context;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
        }

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
            return _context.BureauxDeControle.Any(b => b.Matricule == matricule);
        }

        public User Authenticate(string email, string motDePasse)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email.ToUpper() == email.ToUpper());

            if (user != null)
            {
                if (user.StatutCompte != StatutCompte.ACTIF)
                {
                    throw new UnauthorizedAccessException("Ce compte est désactivé ou suspendu.");
                }

                if (BCrypt.Net.BCrypt.Verify(motDePasse, user.MotDePasseHash))
                {
                    user.DernierLogin = DateTime.UtcNow;
                    Save();
                    return user;
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

        public Citoyen RegisterCitoyen(Citoyen citoyen)
        {
            if (string.IsNullOrWhiteSpace(citoyen.Cin))
                throw new ArgumentException("Le CIN est obligatoire.");

            if (citoyen.Cin.Length != 8 || !citoyen.Cin.All(char.IsDigit))
                throw new ArgumentException("Le CIN doit contenir exactement 8 chiffres.");

            int cinValue = int.Parse(citoyen.Cin);
            if (cinValue < 1 || cinValue > 99999999)
                throw new ArgumentException("Le CIN doit être dans l'intervalle 00000001 et 99999999.");

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
                var domaines = new[]
                {
                    Domaine.ROUTE,
                    Domaine.ECLAIRAGE,
                    Domaine.EAU,
                    Domaine.TRANSPORT,
                    Domaine.ELECTRICITE,
                    Domaine.INFRASTRUCTURE,
                    Domaine.DECHETS
                };

                foreach (var domaine in domaines)
                {
                    var service = new Service
                    {
                        Email = $"{domaine.ToString().ToLower()}.{bureau.Delegation.ToString().ToLower()}@civiclens.tn",
                        MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("Service@123"),
                        Role = Role.SERVICE,
                        StatutCompte = StatutCompte.INACTIF,
                        Domaine = domaine,
                        Region = bureau.Region,
                        Delegation = bureau.Delegation,
                        Nom = $"Service {domaine}",
                        BureauDeControleId = bureau.Id,
                        BureauDeControle = bureau
                    };
                    _context.Services.Add(service);
                }
                Save();
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

        public bool Save()
        {
            var saved = _context.SaveChanges();
            return saved > 0;
        }
    }
}