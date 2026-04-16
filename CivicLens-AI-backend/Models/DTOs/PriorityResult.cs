using System.Text.Json.Serialization;

namespace CivicLens.API.Models.DTOs
{
    public class PriorityResult
    {
        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }
        
        [JsonPropertyName("address")]
        public object? Address { get; set; }
        
        [JsonPropertyName("zone_score")]
        public double ZoneScore { get; set; }
    }
}
