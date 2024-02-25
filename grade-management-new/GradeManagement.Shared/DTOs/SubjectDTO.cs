namespace GradeManagement.Shared.DTOs
{
	public class SubjectDTO
	{
        public long Id { get; set; }
        public string Name { get; set; }
        public string NeptunSubjectCode { get; set; }
        public List<CourseDTO> CourseDtos { get; set; }
	}
}
