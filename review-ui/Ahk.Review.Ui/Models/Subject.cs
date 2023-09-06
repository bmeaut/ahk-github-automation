using DTOs;

namespace Ahk.Review.Ui.Models
{
    public class Subject
    {
        public Subject(SubjectDTO subjectDTO)
        {
            this.Id = subjectDTO.Id;
            this.Name = subjectDTO.Name;
            this.Semester = subjectDTO.Semester;
            this.SubjectCode = subjectDTO.SubjectCode;
            this.GithubOrg = subjectDTO.GithubOrg;
            this.AhkConfig = subjectDTO.AhkConfig;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string Semester { get; set; }
        public string SubjectCode { get; set; }
        public string GithubOrg { get; set; }
        public string AhkConfig { get; set; }
    }
}
