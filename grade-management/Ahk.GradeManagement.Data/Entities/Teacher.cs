using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Teacher : User
    {
        public string Role { get; set; }

        public ICollection<TeacherSubject> TeacherSubjects { get; set; }
        public ICollection<TeacherGroup> TeacherGroups { get; set; }
    }
}
