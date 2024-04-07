namespace GradeManagement.Data.Models;

public class Language : ISoftDelete
{
    public long Id { get; set; }
    public string Name { get; set; }
    public List<Course> Courses { get; set; }
    public bool IsDeleted { get; set; }
}
