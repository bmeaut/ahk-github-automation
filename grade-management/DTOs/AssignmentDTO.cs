using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTOs
{
    public class AssignmentDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DeadLine { get; set; }
        public Uri ClassroomAssignment { get; set; }
        public string SubjectId { get; set; }
    }
}
