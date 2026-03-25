using CivicLens.API.Models.Enums;
using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    /// <summary>
    /// Bureau de Contrôle (hérite de User) - attributs spécifiques.
    /// </summary>
    public class BureauDeControle : User
    {
        public string Matricule { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;

        // Navigation Properties
        public ICollection<Service> Services { get; set; } = new List<Service>();
    }
}