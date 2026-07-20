namespace Ahk.Web.Data;

/// <summary>
/// Supplies the course the current request is scoped to. Implemented in the web layer (resolved from the
/// {course} route segment) and consumed by <see cref="ApplicationDbContext"/> to drive the course query filter.
/// </summary>
public interface ICurrentCourseProvider
{
    Guid? CurrentCourseId { get; }
}

/// <summary>No-op provider (no active course). Used at design time and in host/admin contexts.</summary>
public sealed class NullCurrentCourseProvider : ICurrentCourseProvider
{
    public Guid? CurrentCourseId => null;
}
