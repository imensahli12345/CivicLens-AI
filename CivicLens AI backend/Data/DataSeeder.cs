using CivicLens.API.Models.Entities;
using CivicLens.API.Models.Enums;

namespace CivicLens.API.Data
{
    public static class DataSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // ── SuperAdmin (Id fixe = 1) ──
            if (!context.Users.Any(u => u.Role == Role.SUPERADMIN))
            {
                var superAdmin = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Email = "superadmin@civiclens.tn",
                    MotDePasseHash = BCrypt.Net.BCrypt.HashPassword("SuperAdmin@123"),
                    Adresse = "Tunis",
                    Role = Role.SUPERADMIN,
                    StatutCompte = StatutCompte.ACTIF
                };
                context.Users.Add(superAdmin);
                context.SaveChanges();
            }
        }
    }
}
