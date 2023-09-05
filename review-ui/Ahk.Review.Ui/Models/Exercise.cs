
using DTOs;

namespace Ahk.Review.Ui.Models
{
    public class Exercise
    {
        public Exercise(ExerciseDTO exerciseDTO)
        {
            this.Id = exerciseDTO.Id;
            this.AvailablePoints = exerciseDTO.AvailablePoints;
            this.Name = exerciseDTO.Name;
        }

        public int Id { get; set; }
        public int AvailablePoints { get; set; }
        public string Name { get; set; }
    }
}
