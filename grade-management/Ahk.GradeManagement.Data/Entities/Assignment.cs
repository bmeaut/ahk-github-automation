using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Assignment
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DeadLine { get; set; }
        public Uri ClassroomAssignment { get; set; }
        public ICollection<Exercise> Exercises { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
        public Grade Grade { get; set; }
        public ICollection<StudentAssignment> StudentAssignments { get; set; }

    }
}
