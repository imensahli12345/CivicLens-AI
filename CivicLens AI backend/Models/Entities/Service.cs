using CivicLens.API.Models.Enums;
using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    /// <summary>
    /// Service (hérite de User) - attributs spécifiques.
    /// </summary>
    public class Service : User
    {
        public string Nom { get; set; } = string.Empty;
        public Domaine Domaine { get; set; }

        // Foreign Key
        public Guid BureauDeControleId { get; set; }

        // Navigation Properties
        public BureauDeControle BureauDeControle { get; set; } = null!;
    }
}