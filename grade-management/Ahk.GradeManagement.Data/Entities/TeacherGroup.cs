using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class TeacherGroup
    {
        public int TeacherId { get; set; }
        public int GroupId { get; set; }

        public Teacher Teacher { get; set; }
        public Group Group { get; set; }
    }
}
