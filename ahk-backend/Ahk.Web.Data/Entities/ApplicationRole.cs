using Microsoft.AspNetCore.Identity;

namespace Ahk.Web.Data.Entities;

/// <summary>Application-wide (site-level) role, e.g. the super-admin role. Course-level roles live on <see cref="CourseMembership"/>.</summary>
public class ApplicationRole : IdentityRole<int>
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }
}
