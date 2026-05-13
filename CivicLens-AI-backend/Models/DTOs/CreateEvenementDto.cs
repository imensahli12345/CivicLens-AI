using System;
using System.ComponentModel.DataAnnotations;

namespace CivicLens.API.Models.DTOs
{
    public class CreateEvenementDto
    {
        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime DateEvenement { get; set; }

        [Required]
        [MaxLength(500)]
        public string Lieu { get; set; } = string.Empty;

        [Required]
        public int NombreVolontairesRequis { get; set; }
    }
}
