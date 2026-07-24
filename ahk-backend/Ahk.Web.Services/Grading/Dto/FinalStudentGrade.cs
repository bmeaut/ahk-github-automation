namespace Ahk.Web.Services.Grading.Dto;

/// <summary>
/// One student's final (latest confirmed) grade for a submission. Shape preserved from
/// <c>grade-management/.../ListGrades/Dto/FinalStudentGrade.cs</c>, including the exercise-name → points map
/// that drives the CSV column layout.
/// </summary>
public sealed class FinalStudentGrade
{
    public string Neptun { get; set; } = string.Empty;

    public string Repo { get; set; } = string.Empty;

    public string? PrUrl { get; set; }

    public IReadOnlyDictionary<string, double> Points { get; set; } = new Dictionary<string, double>();
}
