using GradeManagement.Shared.Dtos.Response;

namespace GradeManagement.Shared.Dtos;

public class GroupStudent
{
    public long Id { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
    public Student Student { get; set; }
    public long StudentId { get; set; }
}
