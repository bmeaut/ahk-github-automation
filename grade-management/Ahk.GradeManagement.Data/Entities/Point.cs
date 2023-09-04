using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Data.Entities
{
    public class Point
    {
        public int Id { get; set; }
        public double PointEarned { get; set; }


        public int ExerciseId { get; set; }
        public Exercise Exercise { get; set; }
        public int? GradeId { get; set; }
        public Grade? Grade { get; set; }
    }
}
