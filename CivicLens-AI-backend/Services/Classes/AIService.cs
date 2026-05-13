using CivicLens.API.Models.DTOs;
using CivicLens.API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace CivicLens.API.Services.Classes
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public AIService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<AIAnalysisResponse?> AnalyzeSignalementAsync(string description, double latitude, double longitude, List<IFormFile>? images)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(description), "description");
            content.Add(new StringContent(latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "latitude");
            content.Add(new StringContent(longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)), "longitude");

            if (images != null)
            {
                int count = 1;
                foreach (var img in images.Take(3))
                {
                    var streamContent = new StreamContent(img.OpenReadStream());
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(img.ContentType);
                    content.Add(streamContent, $"image{count}", img.FileName);
                    count++;
                }
            }

            var response = await _httpClient.PostAsync("/analyze", content);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<AIAnalysisResponse>(json, _jsonOptions);
        }

        public async Task<DuplicateDetectionResponse?> CheckDuplicateAsync(string description, double latitude, double longitude, string domain, List<ExistingReportDto> existingReports)
        {
            var req = new DuplicateDetectionRequest
            {
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                Domain = domain,
                Existing_Reports = existingReports ?? new List<ExistingReportDto>()
            };

            var content = new StringContent(JsonSerializer.Serialize(req, _jsonOptions), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/check-duplicate", content);
            
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<DuplicateDetectionResponse>(json, _jsonOptions);
        }

        public async Task<PriorityResult?> UpdatePriorityAsync(string gravity, int voteCount, string? domain, double latitude, double longitude)
        {
            var payload = new
            {
                gravity = gravity,
                vote_count = voteCount,
                domain = domain ?? "AUTRE",
                latitude = latitude,
                longitude = longitude
            };

            var content = new StringContent(JsonSerializer.Serialize(payload, _jsonOptions), System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/update-priority", content);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PriorityResult>(json, _jsonOptions);
        }
    }
}
