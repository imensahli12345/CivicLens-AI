using CivicLens.API.Models.Enums;
using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    /// <summary>
    /// Organisation (hérite de User) - attributs spécifiques.
    /// </summary>
    public class Organisation : User
    {
        public string NomOrganisation { get; set; } = string.Empty;
        public TypeOrganisation TypeOrganisation { get; set; }
        public Domaine DomaineOrganisation { get; set; }
        public string Region { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<Signalement> Signalements { get; set; } = new List<Signalement>();
    }
}