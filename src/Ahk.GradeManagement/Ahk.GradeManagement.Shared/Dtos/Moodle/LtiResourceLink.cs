using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.Moodle;

public class LtiResourceLink
{
    public static readonly string JwtUrl = "https://purl.imsglobal.org/spec/lti/claim/resource_link";

    [JsonPropertyName("title")] public string Title { get; set; }

    [JsonPropertyName("description")] public string Description { get; set; }

    [JsonPropertyName("id")] public string Id { get; set; }
}
