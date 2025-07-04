using Ahk.GradeManagement.Dal.Entities.Interfaces;

namespace Ahk.GradeManagement.Dal.Entities;

public class GroupTeacher : ISoftDelete
{
    public long Id { get; set; }
    public User User { get; set; }
    public long UserId { get; set; }
    public Group Group { get; set; }
    public long GroupId { get; set; }
    public bool IsDeleted { get; set; }
}
