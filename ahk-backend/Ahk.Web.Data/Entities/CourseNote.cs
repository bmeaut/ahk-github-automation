namespace Ahk.Web.Data.Entities;

/// <summary>
/// Temporary probe entity used only to verify course-scoping end-to-end in this skeleton milestone.
/// It is the first <see cref="ICourseScoped"/> type; real domain entities (grades, statuses, submissions)
/// replace it during the port, and this type is then removed.
/// </summary>
public class CourseNote : ICourseScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CourseId { get; set; }

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Course? Course { get; set; }
}
