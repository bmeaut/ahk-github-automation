namespace PublishResult.AppArgs;

public class Exercise(string exerciseName, int maxPoint, int givenPoint)
{
    public string ExerciseName { get; set; } = exerciseName;
    public int MaxPoint { get; set; } = maxPoint;
    public int GivenPoint { get; set; } = givenPoint;

    public override string ToString()
    {
        return $"{ExerciseName}: {GivenPoint} out of {MaxPoint} points.";
    }
}
