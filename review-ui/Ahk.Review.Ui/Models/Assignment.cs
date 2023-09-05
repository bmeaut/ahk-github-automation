using System.Reflection.Metadata.Ecma335;
using DTOs;

namespace Ahk.Review.Ui.Models
{
    public class Assignment
    {
        public Assignment(AssignmentDTO assignmentDTO)
        {
            this.Id = assignmentDTO.Id;
            this.Name = assignmentDTO.Name;
            this.DeadLine = assignmentDTO.DeadLine;
            this.ClassroomAssignment = assignmentDTO.ClassroomAssignment;
            this.Exercises = assignmentDTO.Exercises.Select(eDTO =>
            {
                return new Exercise(eDTO);
            }).ToList();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTimeOffset DeadLine { get; set; }
        public Uri ClassroomAssignment { get; set; }
        public ICollection<Exercise> Exercises { get; set; }
    }
}
