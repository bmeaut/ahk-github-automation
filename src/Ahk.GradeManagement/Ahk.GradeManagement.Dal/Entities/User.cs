using Ahk.GradeManagement.Dal.Entities.Interfaces;
using Ahk.GradeManagement.Shared.Enums;

namespace Ahk.GradeManagement.Dal.Entities;

public class User : ISoftDelete
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GithubId { get; set; }
    public string BmeEmail { get; set; }
    public UserType Type { get; set; }
    public List<PullRequest> PullRequests { get; set; }
    public List<GroupTeacher> GroupTeachers { get; set; }
    public List<SubjectTeacher> SubjectTeachers { get; set; }
    public List<Score> Scores { get; set; }
    public bool IsDeleted { get; set; }
}
