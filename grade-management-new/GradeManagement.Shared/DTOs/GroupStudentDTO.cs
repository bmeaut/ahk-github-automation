namespace GradeManagement.Shared.DTOs;

public class GroupStudentDTO
{
    public long Id { get; set; }
    public GroupDTO GroupDto { get; set; }
    public StudentDTO StudentDto { get; set; }
}
