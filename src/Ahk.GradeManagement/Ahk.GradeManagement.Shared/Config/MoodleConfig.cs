namespace Ahk.GradeManagement.Shared.Config;

public class MoodleConfig
{
    public static readonly string Name = "MoodleConfig";

    public string MoodlePrivateKey { get; set; }

    public static string GetSectionName(string moodleClientId) => Name + ":" + moodleClientId;
}
