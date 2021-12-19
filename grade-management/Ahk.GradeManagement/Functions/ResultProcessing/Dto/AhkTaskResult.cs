using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Ahk.GradeManagement.ResultProcessing.Dto
{
    public class AhkTaskResult
    {
        [JsonPropertyName("exerciseName")]
        public string ExerciseName { get; set; }

        [JsonPropertyName("taskName")]
        [Required]
        [StringLength(maximumLength: 100, MinimumLength = 1)]
        public string TaskName { get; set; }

        [JsonPropertyName("points")]
        [Required]
        public double Points { get; set; }

        [JsonPropertyName("comment")]
        public string Comment { get; set; }
    }
}
