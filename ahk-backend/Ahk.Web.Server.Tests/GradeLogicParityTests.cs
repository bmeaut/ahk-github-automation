using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Grading;
using Ahk.Web.Services.Grading.Dto;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Parity checks for the grade logic ported from grade-management. The expectations mirror
/// <c>ResultProcessor.GetTotalPoints</c> and <c>SetGradeService.getPoints</c>; if these drift, grades change
/// meaning for existing courses.
/// </summary>
public class GradeLogicParityTests
{
    [Fact]
    public void AggregatePoints_SumsTasksPerExercise_OrderedByName()
    {
        var tasks = new[]
        {
            new EvaluationTaskResult { ExerciseName = "ex2", TaskName = "t1", Points = 1 },
            new EvaluationTaskResult { ExerciseName = "ex1", TaskName = "t2", Points = 2 },
            new EvaluationTaskResult { ExerciseName = "ex1", TaskName = "t3", Points = 3 },
        };

        var result = GradeService.AggregatePoints(tasks);

        Assert.Equal(2, result.Count);
        Assert.Equal("ex1", result[0].Name);
        Assert.Equal(5, result[0].Point);   // 2 + 3 summed
        Assert.Equal("ex2", result[1].Name);
        Assert.Equal(1, result[1].Point);
    }

    [Fact]
    public void AggregatePoints_TreatsMissingExerciseNameAsEmptyGroup()
    {
        var tasks = new[]
        {
            new EvaluationTaskResult { ExerciseName = null, TaskName = "t1", Points = 2 },
            new EvaluationTaskResult { ExerciseName = string.Empty, TaskName = "t2", Points = 3 },
        };

        var result = GradeService.AggregatePoints(tasks);

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Name);
        Assert.Equal(5, result[0].Point);
    }

    [Fact]
    public void AggregatePoints_HandlesNoTasks()
        => Assert.Empty(GradeService.AggregatePoints(Array.Empty<EvaluationTaskResult>()));

    [Fact]
    public void BuildPoints_UsesPositionalDefaultNames_WhenNoPreviousResult()
    {
        var result = GradeService.BuildPoints(new[] { 5d, 3.5d, 0d }, previousPoints: null);

        Assert.Equal(new[] { "ex0", "ex1", "ex2" }, result.Select(p => p.Name));
        Assert.Equal(new[] { 5d, 3.5d, 0d }, result.Select(p => p.Point));
    }

    [Fact]
    public void BuildPoints_CarriesForwardPreviousExerciseNames()
    {
        var previous = new List<GradeExercisePoint>
        {
            new() { Name = "Exercise 1", Point = 1, Order = 0 },
            new() { Name = "Exercise 2", Point = 2, Order = 1 },
        };

        var result = GradeService.BuildPoints(new[] { 5d, 4d }, previous);

        Assert.Equal(new[] { "Exercise 1", "Exercise 2" }, result.Select(p => p.Name));
        Assert.Equal(new[] { 5d, 4d }, result.Select(p => p.Point));
    }

    [Fact]
    public void BuildPoints_FallsBackToPositionalNames_BeyondPreviousCount()
    {
        var previous = new List<GradeExercisePoint> { new() { Name = "Exercise 1", Point = 1, Order = 0 } };

        var result = GradeService.BuildPoints(new[] { 5d, 4d }, previous);

        Assert.Equal(new[] { "Exercise 1", "ex1" }, result.Select(p => p.Name));
    }

    [Fact]
    public void CsvExporter_PreservesLegacyFormat()
    {
        var grades = new[]
        {
            new FinalStudentGrade
            {
                Neptun = "abc123",
                Repo = "org/course-hw1-abc123",
                PrUrl = "https://github.com/org/course-hw1-abc123/pull/1",
                Points = new Dictionary<string, double> { ["ex0"] = 2, ["ex1"] = 3.5 },
            },
            new FinalStudentGrade
            {
                Neptun = "XYZ789",
                Repo = "org/course-hw1-xyz789",
                PrUrl = null,
                Points = new Dictionary<string, double> { ["ex0"] = 1 },
            },
        };

        var csv = CsvExporter.GetCsv(grades);
        var lines = csv.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("Neptun;GitHubRepo;GitHubPr;ex0;ex1", lines[0]);
        Assert.Equal("ABC123;org/course-hw1-abc123;https://github.com/org/course-hw1-abc123/pull/1;2;3.5", lines[1]);

        // Missing exercise -> empty cell; missing PR url -> empty cell.
        Assert.Equal("XYZ789;org/course-hw1-xyz789;;1;", lines[2]);
    }
}
