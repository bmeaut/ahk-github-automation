namespace Ahk.Review.Ui.Models
{
    public class Assignment
    {
        public Assignment()
        {

        }

        public string Name { get; set; }
        public DateTimeOffset DeadLine { get; set; }
        public Uri ClassroomAssignment { get; set; }
        public ICollection<Exercise> Exercises { get; set; }
    }
}
