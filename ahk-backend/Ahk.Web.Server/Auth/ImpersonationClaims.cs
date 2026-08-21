namespace Ahk.Web.Server.Auth;

/// <summary>
/// Marks a session as an admin impersonating someone else, and remembers who to return to.
///
/// <para>These claims live <b>only</b> in the ASP.NET Identity application cookie, which data protection
/// encrypts and signs — that is the whole security model: a client cannot forge or transplant one, so the
/// presence of <see cref="ImpersonatorId"/> is proof that a site admin started this session through
/// <see cref="ImpersonationController"/>. They are never written to <c>AspNetUserClaims</c>.</para>
/// </summary>
public static class ImpersonationClaims
{
    /// <summary>User id of the site admin who started the impersonation.</summary>
    public const string ImpersonatorId = "ahk:impersonator_id";

    /// <summary>User name of that admin, so the banner can name the account to return to without a lookup.</summary>
    public const string ImpersonatorName = "ahk:impersonator_name";
}
