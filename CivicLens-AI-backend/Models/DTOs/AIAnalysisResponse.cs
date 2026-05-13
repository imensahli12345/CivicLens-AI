using System.Text.Json.Serialization;

namespace CivicLens.API.Models.DTOs
{
    public class AIAnalysisResponse
    {
        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("low_confidence")]
        public bool Low_Confidence { get; set; }

        [JsonPropertyName("gravity")]
        public string Gravity { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public string Priority { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("address")]
        public object? Address { get; set; }
    }
}
