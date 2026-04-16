using System;
using System.ComponentModel.DataAnnotations;

namespace CivicLens.API.Models.DTOs
{
    public class UpdateEvenementDto
    {
        [Required]
        public DateTime DateEvenement { get; set; }

        [Required]
        [MaxLength(500)]
        public string Lieu { get; set; } = string.Empty;
    }
}

