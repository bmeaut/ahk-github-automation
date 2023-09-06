using DTOs;

namespace Ahk.Review.Ui.Models
{
    public class Student
    {
        public Student(StudentDTO studentDTO)
        {
            this.Id = studentDTO.Id;
            this.Name = studentDTO.Name;
            this.Neptun = studentDTO.Neptun;
            this.GithubUser = studentDTO.GithubUser;
            this.EduEmail = studentDTO.EduEmail;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Neptun { get; set; }
        public string EduEmail { get; set; }
        public string GithubUser { get; set; }
    }
}
