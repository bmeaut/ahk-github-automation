namespace GradeManagement.Shared.DTOs;

public class StudentDTO
{
    public long Id { get; set; }
    public string Name { get; set; }
    public string NeptunCode { get; set; }
    public List<GroupStudentDTO> GroupStudentDtos { get; set; }
    public List<AssignmentDTO> AssignmentDtos { get; set; }
}
