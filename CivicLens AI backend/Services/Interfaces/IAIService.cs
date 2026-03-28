using CivicLens.API.Models.DTOs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CivicLens.API.Services.Interfaces
{
    public interface IAIService
    {
        Task<AIAnalysisResponse?> AnalyzeSignalementAsync(string description, double latitude, double longitude, List<IFormFile>? images);
        Task<DuplicateDetectionResponse?> CheckDuplicateAsync(string description, double latitude, double longitude, string domain, List<ExistingReportDto> existingReports);
        Task<PriorityResult?> UpdatePriorityAsync(string gravity, int voteCount, string? domain, double latitude, double longitude);
    }
}
