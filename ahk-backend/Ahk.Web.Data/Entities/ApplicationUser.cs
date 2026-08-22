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
    /// Neptun code — from the IdP's <c>neptun_code</c> claim, or set by an admin when creating the account.
    /// The key of the domain model: it links a signed-in user to their <see cref="Student"/> rows and is how
    /// an eduID login is matched to a pre-provisioned account. Unique when present (filtered unique index);
    /// <c>null</c> means "no code" and may repeat.
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

    /// <summary>
    /// When the claim to <see cref="GitHubUsername"/> was last corroborated, or null while it is only the
    /// user's own assertion. Set the moment a repository invitation sent to that login is accepted, because
    /// only someone signed in as that account can accept one.
    ///
    /// <para>Corroboration, not proof of ownership: a user who types a stranger's login still causes an
    /// invitation to be sent to the stranger, and if the stranger accepts it this is stamped anyway. It says
    /// "somebody holding this login acted on it", which is what makes a mistyped login self-correcting — it
    /// simply never becomes verified. Cleared whenever the stored login changes.</para>
    /// </summary>
    public DateTimeOffset? GitHubVerifiedAt { get; set; }

    public ICollection<CourseMembership> CourseMemberships { get; } = new List<CourseMembership>();
}
