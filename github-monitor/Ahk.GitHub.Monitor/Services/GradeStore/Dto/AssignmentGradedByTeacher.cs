using System;
using System.Collections.Generic;
using Ahk.GitHub.Monitor.Services.StatusTrackingStore.Dto;

namespace Ahk.GitHub.Monitor.Services.GradeStore.Dto;

public class AssignmentGradedByTeacher(
    string gitHubRepositoryUrl,
    string pullRequestUrl,
    string teacherGitHubId,
    Dictionary<int, double> scores,
    DateTimeOffset dateOfGrading) : StatusEventBase(gitHubRepositoryUrl)
{
    public string PullRequestUrl { get; set; } = pullRequestUrl;
    public string TeacherGitHubId { get; set; } = teacherGitHubId;
    public Dictionary<int, double> Scores { get; set; } = scores; // order, score
    public DateTimeOffset DateOfGrading { get; set; } = dateOfGrading;
}
