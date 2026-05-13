using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CivicLens.API.Models.DTOs
{
    public class CreateSignalementDto
    {
        [Required]
        [MinLength(5)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Latitude { get; set; } = string.Empty;

        [Required]
        public string Longitude { get; set; } = string.Empty;

        public List<IFormFile>? Images { get; set; }
    }
}
