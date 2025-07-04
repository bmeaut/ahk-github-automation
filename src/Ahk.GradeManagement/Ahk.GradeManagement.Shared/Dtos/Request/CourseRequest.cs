using Ahk.GradeManagement.Shared.Dtos;

namespace Ahk.GradeManagement.Shared.Dtos.Request;

public class CourseRequest
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleClientId { get; set; }
    public long SubjectId { get; set; }
    public Semester Semester { get; set; }
    public Language Language { get; set; }
}
