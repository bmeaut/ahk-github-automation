using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.Moodle;

public class LtiLineItemRequest
{
    [JsonPropertyName("scoreMaximum")]
    public int ScoreMaximum { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("resourceId")]
    public string ResourceId { get; set; } = "";

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = "";

    [JsonPropertyName("startDateTime")]
    public DateTime StartDateTime { get; set; }

    [JsonPropertyName("endDateTime")]
    public DateTime EndDateTime { get; set; }
}
