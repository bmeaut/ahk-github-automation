namespace Ahk.Web.Data.Entities;

/// <summary>Role a user holds within a specific course (distinct from site-level roles).</summary>
public enum CourseRole
{
    /// <summary>Can view the course's submissions, statuses and grades.</summary>
    Instructor = 0,

    /// <summary>Can additionally manage the course's configuration and members.</summary>
    Admin = 1,
}
