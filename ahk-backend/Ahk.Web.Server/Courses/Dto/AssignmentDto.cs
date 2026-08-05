using Ahk.Web.Services.Assignments;

namespace Ahk.Web.Server.Courses.Dto;

/// <summary>An assignment as the instructor screens see it.</summary>
public sealed class AssignmentDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Full "owner/name" of the template repository students are given a copy of.</summary>
    public string TemplateRepoName { get; set; } = string.Empty;

    /// <summary>
    /// The invite link as a site-relative path (<c>/{course}/invite/{token}</c>). Deliberately not an absolute
    /// URL: the API would have to build one from the Host header, and the Angular dev proxy rewrites that to
    /// the backend's own port — producing a link that works for nobody. The browser knows the origin the
    /// student will actually use, so it composes the final URL.
    /// </summary>
    public string InvitePath { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>How many students have taken this assignment up.</summary>
    public int AcceptanceCount { get; set; }
}

/// <summary>An assignment plus the state of its template repository on GitHub.</summary>
public sealed class AssignmentDetailDto
{
    public AssignmentDto Assignment { get; set; } = new();

    /// <summary>Null when the template was not checked (the check costs a GitHub call, so it is opt-in).</summary>
    public TemplateCheckDto? Template { get; set; }
}

/// <summary>Advisory result of looking the template repository up on GitHub.</summary>
public sealed class TemplateCheckDto
{
    public bool Reachable { get; set; }

    public bool IsTemplate { get; set; }

    public string? HtmlUrl { get; set; }

    /// <summary>What is wrong, in one sentence; null when the template is fine.</summary>
    public string? Problem { get; set; }

    public static TemplateCheckDto From(TemplateCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);

        return new TemplateCheckDto
        {
            Reachable = check.Reachable,
            IsTemplate = check.IsTemplate,
            HtmlUrl = check.HtmlUrl,
            Problem = check.Problem,
        };
    }
}

public sealed class SaveAssignmentRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>"owner/name", or a bare repository name taken to be in the course's organization.</summary>
    public string TemplateRepoName { get; set; } = string.Empty;
}

/// <summary>
/// A template repository to look up on GitHub without saving an assignment first — lets the editor validate
/// a name while creating, before an assignment id exists. A body (not a query param) because the name may
/// contain a '/'.
/// </summary>
public sealed class CheckTemplateRequest
{
    public string? TemplateRepoName { get; set; }
}

/// <summary>One student's acceptance, as the instructor's roster of an assignment shows it.</summary>
public sealed class AssignmentAcceptanceDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public string? NeptunCode { get; set; }

    public string GitHubUsername { get; set; } = string.Empty;

    public string GitHubRepoName { get; set; } = string.Empty;

    public string RepoUrl { get; set; } = string.Empty;

    public DateTimeOffset AcceptedAt { get; set; }

    /// <summary>True while the student has been invited to their repository but has not accepted yet.</summary>
    public bool InvitationPending { get; set; }
}
