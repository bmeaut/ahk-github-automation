namespace Ahk.Web.Data.Entities;

/// <summary>
/// Marker for domain entities whose rows belong to a single course. The <see cref="ApplicationDbContext"/>
/// applies an EF Core global query filter on <see cref="CourseId"/> so queries only see the active course's
/// rows. Authorization (course membership) remains the real access gate; the filter is a scoping convenience.
/// </summary>
public interface ICourseScoped
{
    Guid CourseId { get; }
}
