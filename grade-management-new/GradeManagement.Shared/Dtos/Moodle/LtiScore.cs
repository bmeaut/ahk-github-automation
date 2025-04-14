using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.Moodle;

public class LtiScore
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    [JsonPropertyName("scoreGiven")]
    public double ScoreGiven { get; set; }

    [JsonPropertyName("scoreMaximum")]
    public double ScoreMaximum { get; set; }

    /*[JsonPropertyName("comment")]
    public string Comment { get; set; }*/

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }

    [JsonPropertyName("activityProgress")]
    public string ActivityProgress { get; set; }

    [JsonPropertyName("gradingProgress")]
    public string GradingProgress { get; set; }
}

