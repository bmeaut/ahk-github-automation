using System;
using System.Collections.Generic;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

namespace Ahk.GitHub.Monitor.Services.GradeStore.Dto;

public class AssignmentGradedByTeacher : StatusEventBase
{
    public required string PullRequestUrl { get; init; }
    public required string TeacherGitHubId { get; init; }
    public required Dictionary<int, double> Scores { get; init; } // order, score
    public required DateTimeOffset DateOfGrading { get; init; }
}
