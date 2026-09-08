using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Assignments;
using Ahk.Web.Services.GitHub;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ahk.Web.Server.Auth;

public sealed class SetGitHubUsernameRequest
{
    public string GitHubUsername { get; set; } = string.Empty;

    /// <summary>
    /// Optional course whose GitHub App token pays for the lookup. Purely a rate-limit matter — the call works
    /// unauthenticated too, at 60 requests an hour instead of 5000. The invite page always has a course to name.
    /// </summary>
    public string? CourseSlug { get; set; }
}

public sealed class GitHubProfileResponse
{
    public string GitHubUsername { get; set; } = string.Empty;

    public long? GitHubUserId { get; set; }

    /// <summary>
    /// False while the link is only the user's own claim. It turns true once an invitation sent to that login
    /// has been accepted — which is the first assignment they take — so a freshly entered name is always false.
    /// </summary>
    public bool Verified { get; set; }

    /// <summary>
    /// Repositories the corrected login was just invited to. Zero on a first entry (there are none yet) and on
    /// a re-entry of the same login (nothing changed).
    /// </summary>
    public int RepositoriesShared { get; set; }

    /// <summary>
    /// Repositories the invitation could not be sent for — GitHub unreachable, or a course with no working App.
    /// They keep the old login and can be re-sent from the student's own page.
    /// </summary>
    public int RepositoriesFailed { get; set; }
}

/// <summary>
/// The parts of a user's own profile they maintain themselves. Today that is one thing: their GitHub account,
/// which the portal needs before it can hand them a repository.
/// </summary>
[ApiController]
[Route("api/profile")]
[Authorize]
public sealed class ProfileController : ControllerBase
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IGitHubRepositoryService gitHub;
    private readonly ICourseGitHubAppTokenProvider tokens;
    private readonly ApplicationDbContext db;
    private readonly IStudentAssignmentService studentAssignments;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IGitHubRepositoryService gitHub,
        ICourseGitHubAppTokenProvider tokens,
        ApplicationDbContext db,
        IStudentAssignmentService studentAssignments)
    {
        this.userManager = userManager;
        this.gitHub = gitHub;
        this.tokens = tokens;
        this.db = db;
        this.studentAssignments = studentAssignments;
    }

    /// <summary>
    /// Records the caller's GitHub login, after checking it exists. The check is <c>GET /users/{login}</c>
    /// rather than a fetch of the profile page: a 404 there is unambiguous, and the response carries the
    /// numeric account id, which survives the user renaming themselves later.
    ///
    /// <para>Correcting a misspelling is the reason this can be called twice, so a changed login is followed by
    /// re-sharing every repository the student already holds with the new account — the earlier invitations went
    /// to whoever owns the name they typed first, and those are left alone (that stranger can only decline).</para>
    ///
    /// <para>⚠️ A <b>confirmed</b> account cannot be changed here. Confirmation means someone signed in as that
    /// account and accepted an invitation, so the link is no longer a typo waiting to be fixed — and letting it
    /// be re-pointed would be a way to hand a stranger's account the repositories it earned.</para>
    /// </summary>
    [HttpPut("github")]
    [ProducesResponseType(typeof(GitHubProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GitHubProfileResponse>> SetGitHubUsername([FromBody] SetGitHubUsernameRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        // A pasted profile URL is the obvious mistake to be forgiving about; everything else must be a login.
        var login = request.GitHubUsername?.Trim().TrimEnd('/') ?? string.Empty;
        var lastSlash = login.LastIndexOf('/');
        if (lastSlash >= 0)
            login = login[(lastSlash + 1)..];

        if (string.IsNullOrWhiteSpace(login))
            return BadRequest(new { error = "Enter your GitHub username." });

        string? token = null;
        if (!string.IsNullOrWhiteSpace(request.CourseSlug))
        {
            var courseId = await db.Courses.AsNoTracking()
                .Where(c => c.Slug == request.CourseSlug)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (courseId != 0)
                token = (await tokens.GetForCourseAsync(courseId, bypassCache: false, cancellationToken))?.Token;
        }

        GitHubUser? account;
        try
        {
            account = await gitHub.GetUserAsync(login, token, cancellationToken);
        }
        catch (Exception ex) when (ex is GitHubOperationException or HttpRequestException or TaskCanceledException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "GitHub could not be reached to check that username. Try again in a few minutes." });
        }

        if (account is null)
            return BadRequest(new { error = $"There is no GitHub user called \"{login}\". Check the spelling — it is the name in your profile URL, github.com/<username>." });

        // One GitHub account belongs to one person, so it may back only one portal account. Matched on the
        // numeric id as well as the login: the id is what survives a rename, so it catches the case where
        // someone re-types a login another account claimed before that account was renamed. The filtered
        // unique indexes are the real guarantee; this check exists to answer with a sentence rather than a 500.
        var takenBy = await db.Users
            .AsNoTracking()
            .Where(u => u.Id != user.Id && (u.GitHubUsername == account.Login || u.GitHubUserId == account.Id))
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (takenBy != 0)
            return BadRequest(new { error = $"The GitHub account \"{account.Login}\" is already linked to another user here. If it is yours, ask an administrator to sort it out." });

        // Two different questions. "Is this a different account?" decides whether the confirmed link is being
        // re-pointed — matched on the numeric id as well, so an account renamed on GitHub is still the same one.
        // "Did the stored login change?" decides whether the repositories have to be shared again, which a
        // rename also needs: the acceptance rows address the collaborator by login.
        var hadLogin = !string.IsNullOrWhiteSpace(user.GitHubUsername);
        var loginChanged = !string.Equals(user.GitHubUsername, account.Login, StringComparison.OrdinalIgnoreCase);
        var sameAccount = !loginChanged || (user.GitHubUserId is not null && user.GitHubUserId == account.Id);

        if (!sameAccount && user.GitHubVerifiedAt is not null)
        {
            return BadRequest(new
            {
                error = $"Your GitHub account \"{user.GitHubUsername}\" is confirmed — you have accepted a repository invitation with it — so it can no longer be changed here. Ask your instructor if it is wrong.",
            });
        }

        // Re-binding to a different account withdraws whatever the previous one had corroborated: the new
        // login is an assertion again until an invitation sent to it is accepted.
        if (!sameAccount)
            user.GitHubVerifiedAt = null;

        // Store GitHub's own casing, so the value shown back matches the account exactly.
        user.GitHubUsername = account.Login;
        user.GitHubUserId = account.Id;

        IdentityResult result;
        try
        {
            result = await userManager.UpdateAsync(user);
        }
        catch (DbUpdateException)
        {
            // Two people claiming the same login at once: the index caught what the check above raced past.
            return BadRequest(new { error = $"The GitHub account \"{account.Login}\" is already linked to another user here." });
        }

        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        // The repositories already created for this student were shared with the login they are correcting, so
        // the new account has no access to any of them until it is invited too. Failures are counted rather
        // than thrown: the login is saved either way, and an uninvited repository is recoverable from /my.
        // A first entry skips it — no login, therefore no repository, therefore nothing to look up.
        var share = hadLogin && loginChanged
            ? await studentAssignments.ShareExistingRepositoriesAsync(user.Id, cancellationToken)
            : default;

        return Ok(new GitHubProfileResponse
        {
            GitHubUsername = account.Login,
            GitHubUserId = account.Id,
            Verified = user.GitHubVerifiedAt is not null,
            RepositoriesShared = share.Shared,
            RepositoriesFailed = share.Failed,
        });
    }
}
