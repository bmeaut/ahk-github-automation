using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Student : User
    {
        public ICollection<StudentAssignment> StudentAssignments { get; set; }
        public ICollection<StudentGroup> StudentGroups { get; set; }
        public ICollection<StudentSubject> StudentSubjects { get; set; }
        public ICollection<Grade>? Grades { get; set; }
    }
}
