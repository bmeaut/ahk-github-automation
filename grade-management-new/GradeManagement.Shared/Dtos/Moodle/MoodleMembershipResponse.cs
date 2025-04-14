namespace GradeManagement.Shared.Dtos.Moodle;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class MembershipResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("context")]
    public Context Context { get; set; }

    [JsonPropertyName("members")]
    public List<Member> Members { get; set; }
}

public class Context
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("label")]
    public string Label { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}

public class Member
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("roles")]
    public List<string> Roles { get; set; }

    [JsonPropertyName("user_id")]
    public string UserId { get; set; }

    [JsonPropertyName("lis_person_sourcedid")]
    public string LisPersonSourcedId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("given_name")]
    public string GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string FamilyName { get; set; }

    [JsonPropertyName("email")]
    public string Email { get; set; }

    [JsonPropertyName("ext_user_username")]
    public string ExtUserUsername { get; set; }
}

