using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CivicLens.API.Models.DTOs
{
    public class UploadRapportDto
    {
        [Required]
        public IFormFile Rapport { get; set; } = default!;
    }
}
