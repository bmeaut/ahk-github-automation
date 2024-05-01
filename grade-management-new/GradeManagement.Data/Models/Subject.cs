namespace GradeManagement.Data.Models;

public class Subject : ISoftDelete
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GitHubOrgName { get; set; }
    public List<Course> Courses { get; set; }
    public List<SubjectTeacher> SubjectTeachers { get; set; }
    public bool IsDeleted { get; set; }
}
