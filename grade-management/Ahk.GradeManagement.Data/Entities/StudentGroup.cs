using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class StudentGroup
    {
        public int StudentId { get; set; }
        public int GroupId { get; set; }

        public Student Student { get; set; }
        public Group Group { get; set; }
    }
}
