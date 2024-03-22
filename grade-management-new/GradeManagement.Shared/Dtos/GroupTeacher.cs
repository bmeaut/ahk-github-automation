using GradeManagement.Shared.Dtos.Response;

namespace GradeManagement.Shared.Dtos;

public class GroupTeacher
{
    public long Id { get; set; }
    public User User { get; set; }
    public long TeacherId { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
}
