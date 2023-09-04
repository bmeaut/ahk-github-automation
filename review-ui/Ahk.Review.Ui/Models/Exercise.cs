
namespace Ahk.Review.Ui.Models
{
    public class Exercise
    {
        public int AvailablePoints { get; set; }
        public string Name { get; set; }
        public Assignment Assignment { get; set; }
        public Point? Point { get; set; }
    }
}
