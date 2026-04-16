using System;
using System.Collections.Generic;

namespace CivicLens.API.Models.DTOs
{
    public class EvenementDto
    {
        public Guid Id { get; set; }
        public string Titre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateEvenement { get; set; }
        public string Lieu { get; set; } = string.Empty;
        public string Delegation { get; set; } = string.Empty;
        public int NombreVolontairesRequis { get; set; }
        public Guid OrganisateurId { get; set; }
        public string OrganisateurNom { get; set; } = string.Empty;
        public int NombreParticipations { get; set; }
        public List<ParticipationDto> Participations { get; set; } = new List<ParticipationDto>();

        public bool IsConfirmed { get; set; }
        public DateTime? ConfirmedAt { get; set; }
    }
}
