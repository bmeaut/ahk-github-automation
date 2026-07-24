namespace Ahk.Web.Data.Entities;

/// <summary>Assigns a user to a course with a course-level role. A user may belong to many courses.</summary>
public class CourseMembership
{
    public int UserId { get; set; }

    public int CourseId { get; set; }

    public CourseRole Role { get; set; } = CourseRole.Instructor;

    public ApplicationUser? User { get; set; }

    public Course? Course { get; set; }
}
