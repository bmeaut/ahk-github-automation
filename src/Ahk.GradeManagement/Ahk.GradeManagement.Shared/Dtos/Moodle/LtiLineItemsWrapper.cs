using System.Text.Json.Serialization;

namespace Ahk.GradeManagement.Shared.Dtos.Moodle;

public class LtiLineItemsWrapper
{
    public static readonly string JwtUrl = "https://purl.imsglobal.org/spec/lti-ags/claim/endpoint";

    [JsonPropertyName("scope")] public List<string> Scope { get; set; }

    [JsonPropertyName("lineitems")] public string LineItems { get; set; }

    [JsonPropertyName("lineitem")] public string LineItem { get; set; }
}
