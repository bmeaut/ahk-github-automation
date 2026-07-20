using Ahk.Web.Data;

namespace Ahk.Web.Server.CourseContext;

/// <summary>
/// Scoped per request. <see cref="CourseResolutionMiddleware"/> sets the active course from the {course} route
/// segment; <see cref="ApplicationDbContext"/> reads it to apply the course query filter.
/// </summary>
public sealed class CurrentCourseProvider : ICurrentCourseProvider
{
    public Guid? CurrentCourseId { get; private set; }

    public void Set(Guid courseId) => CurrentCourseId = courseId;
}
