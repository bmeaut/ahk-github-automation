using Microsoft.AspNetCore.Identity;

namespace Ahk.Web.Data.Entities;

/// <summary>
/// Application user. Extends the ASP.NET Identity user with app-specific profile fields.
/// A user may be a member of many <see cref="Course"/>s via <see cref="CourseMembership"/>.
/// </summary>
public class ApplicationUser : IdentityUser<int>
{
    public string? DisplayName { get; set; }

    /// <summary>
    /// Neptun code from the IdP's <c>neptun_code</c> claim. Not unique here (a directory account may exist
    /// without one), but it is the key of the domain model, so storing it allows linking a signed-in user to
    /// their <see cref="Student"/> rows later.
    /// </summary>
    public string? NeptunCode { get; set; }

    /// <summary>
    /// The IdP's <c>eduperson_scoped_affiliation</c> claim (e.g. "staff@bme.hu"). Multi-valued at the source;
    /// all values are stored joined with ';'. Kept so login can be restricted by affiliation later.
    /// </summary>
    public string? Affiliation { get; set; }

    /// <summary>
    /// GitHub login, verified against the GitHub API when the user first supplies it. Site-wide rather than
    /// per-course: a person has one GitHub account, so once it is known no course asks for it again.
    /// Copied onto <see cref="Student.GitHubUsername"/> when an assignment is accepted.
    /// </summary>
    public string? GitHubUsername { get; set; }

    /// <summary>GitHub's numeric account id — stable across a rename, which the login is not.</summary>
    public long? GitHubUserId { get; set; }

    public ICollection<CourseMembership> CourseMemberships { get; } = new List<CourseMembership>();
}
