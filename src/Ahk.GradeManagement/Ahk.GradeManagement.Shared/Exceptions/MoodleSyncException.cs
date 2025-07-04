namespace Ahk.GradeManagement.Shared.Exceptions;

public class MoodleSyncException : Exception
{
    public MoodleSyncException() : base("Error happened during moodle operation!")
    {
    }

    public MoodleSyncException(string message) : base(message)
    {
    }

    public MoodleSyncException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
