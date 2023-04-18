using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Exercise
    {
        public int Id { get; set; }
        public int AssignmentId { get; set; }
        public int AvailablePoints { get; set; }

        public Assignment Assignment { get; set; }
        public Point? Point { get; set; }
    }
}
