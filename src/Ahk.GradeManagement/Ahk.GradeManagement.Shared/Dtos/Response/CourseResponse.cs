namespace Ahk.GradeManagement.Shared.Dtos.Response;

public class CourseResponse
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string MoodleClientId { get; set; }
    public string PublicKey { get; set; }
    public long SubjectId { get; set; }
    public Semester Semester { get; set; }
    public Language Language { get; set; }
}
