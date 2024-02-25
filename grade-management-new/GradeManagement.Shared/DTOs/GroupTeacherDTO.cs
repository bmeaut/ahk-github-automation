namespace GradeManagement.Shared.DTOs;

public class GroupTeacherDTO
{
    public long Id { get; set; }
    public GroupDTO GroupDto { get; set; }
    public TeacherDTO TeacherDto { get; set; }
}
