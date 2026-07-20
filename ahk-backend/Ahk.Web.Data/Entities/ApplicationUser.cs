using Microsoft.AspNetCore.Identity;

namespace Ahk.Web.Data.Entities;

/// <summary>
/// Application user. Extends the ASP.NET Identity user with app-specific profile fields.
/// A user may be a member of many <see cref="Course"/>s via <see cref="CourseMembership"/>.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }

    public ICollection<CourseMembership> CourseMemberships { get; } = new List<CourseMembership>();
}
