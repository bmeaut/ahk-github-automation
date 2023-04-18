using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Semester { get; set; }
        public string SubjectCode { get; set; }
        public string GithubOrg { get; set; }
        public string ahkConfig { get; set; }

        public ICollection<Assignment> Assignments { get; set; }
        public ICollection<StudentSubject> StudentSubjects { get; set; }
        public ICollection<TeacherSubject> TeacherSubjects { get; set; }
    }
}
