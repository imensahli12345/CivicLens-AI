using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CivicLens.API.Models.DTOs
{
    public class ExistingReportDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("vote_count")]
        public int Vote_Count { get; set; } = 0;
    }

    public class DuplicateDetectionRequest
    {
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("domain")]
        public string Domain { get; set; } = string.Empty;

        [JsonPropertyName("existing_reports")]
        public List<ExistingReportDto> Existing_Reports { get; set; } = new();
    }

    public class DuplicateDetectionResponse
    {
        [JsonPropertyName("is_duplicate")]
        public bool Is_Duplicate { get; set; }

        [JsonPropertyName("original_report_id")]
        public long? Original_Report_Id { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
