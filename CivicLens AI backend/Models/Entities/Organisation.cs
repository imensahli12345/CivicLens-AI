using CivicLens.API.Models.Enums;
using System.Collections.Generic;

namespace CivicLens.API.Models.Entities
{
    public class Organisation : User
    {
        public string NomOrganisation { get; set; } = string.Empty;
        public TypeOrganisation TypeOrganisation { get; set; }
        public Domaine DomaineOrganisation { get; set; }
        public string Region { get; set; } = string.Empty;

        public ICollection<Signalement> Signalements { get; set; } = new List<Signalement>();
    }
}