using System.Text.Json.Serialization;

namespace GradeManagement.Shared.Dtos.Moodle;

public class LtiLisClaim
{
    public static readonly string JwtUrl = "https://purl.imsglobal.org/spec/lti/claim/lis";

    [JsonPropertyName("person_sourcedid")]
    public string PersonSourcedId { get; set; }

    [JsonPropertyName("course_section_sourcedid")]
    public string CourseSectionSourcedId { get; set; }
}
