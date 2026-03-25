using CivicLens.API.Models.Enums;
using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string MotDePasseHash { get; set; } = string.Empty;
        public string Adresse { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; } = DateTime.UtcNow;
        public DateTime? DernierLogin { get; set; }
        public StatutCompte StatutCompte { get; set; } = StatutCompte.ACTIF;
        public Role Role { get; set; }
    }
}