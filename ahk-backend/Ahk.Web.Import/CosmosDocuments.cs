using System.Text.Json.Serialization;

namespace Ahk.Web.Import;

/// <summary>
/// Shapes of the exported CosmosDB documents. Only the fields we import are declared; Cosmos system fields
/// (_rid, _self, _etag, _ts, _attachments) are simply ignored by the deserializer.
/// </summary>
internal sealed class StudentResultDocument
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    public string? Neptun { get; set; }

    public string? GitHubRepoName { get; set; }

    public int? GitHubPrNumber { get; set; }

    public string? GitHubPrUrl { get; set; }

    public DateTimeOffset Date { get; set; }

    public string? Actor { get; set; }

    public string? Origin { get; set; }

    public List<ExerciseWithPointDocument>? Points { get; set; }

    public bool Confirmed { get; set; }
}

internal sealed class ExerciseWithPointDocument
{
    public string? Name { get; set; }

    public double Point { get; set; }
}

/// <summary>
/// One document from the <c>events</c> container. The container is polymorphic: <c>$type</c> selects which
/// subtype's fields are populated (written by the legacy StatusEventItemJsonConverter).
/// </summary>
internal sealed class StatusEventDocument
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("$type")]
    public string? Type { get; set; }

    public string? Repository { get; set; }

    public DateTimeOffset Timestamp { get; set; }

    // BranchCreateEvent
    public string? Branch { get; set; }

    // WorkflowRunEvent
    public string? Conclusion { get; set; }

    // PullRequestEvent
    public string? Action { get; set; }

    public List<string>? Assignees { get; set; }

    public string? Neptun { get; set; }

    public string? HtmlUrl { get; set; }

    public int Number { get; set; }
}

internal sealed class WebhookTokenDocument
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    public string? Secret { get; set; }

    public string? Description { get; set; }
}

/// <summary>Legacy $type discriminator values from the events container.</summary>
internal static class LegacyEventTypes
{
    public const string RepositoryCreate = "RepositoryCreateEvent";
    public const string BranchCreate = "BranchCreateEvent";
    public const string PullRequest = "PullRequestEvent";
    public const string WorkflowRun = "WorkflowRunEvent";
}
