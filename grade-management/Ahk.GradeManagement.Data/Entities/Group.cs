using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Group
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Room { get; set; }
        public string Time { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
        public ICollection<StudentGroup> StudentGroups { get; set; }
        public ICollection<TeacherGroup> TeacherGroups { get; set; }
    }
}
