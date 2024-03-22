using GradeManagement.Shared.Enums;

namespace GradeManagement.Data.Models;

public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public UserType Type { get; set; }
    public List<GroupTeacher> GroupTeachers { get; set; }
    public List<SubjectTeacher> SubjectTeachers { get; set; }
}
