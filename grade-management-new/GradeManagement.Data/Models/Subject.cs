using GradeManagement.Data.Models.Interfaces;

using System.ComponentModel.DataAnnotations;

namespace GradeManagement.Data.Models;

public class Subject : ISoftDelete, ITenant
{
    [Key] public long SubjectId { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public string GitHubOrgName { get; set; }
    public List<Course> Courses { get; set; }
    public List<SubjectTeacher> SubjectTeachers { get; set; }
    public bool IsDeleted { get; set; }
    public string CiApiKey { get; set; }
}
