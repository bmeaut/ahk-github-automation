using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Shared.Dtos.AssignmentEvents;
using GradeManagement.Shared.Enums;

using Microsoft.IdentityModel.Tokens;

using Assignment = GradeManagement.Shared.Dtos.Assignment;
using PullRequest = GradeManagement.Shared.Dtos.PullRequest;

namespace GradeManagement.Bll.Services;

public class AssignmentEventProcessorService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly AssignmentService _assignmentService;
    private readonly AssignmentLogService _assignmentLogService;
    private readonly ExerciseService _exerciseService;
    private readonly StudentService _studentService;
    private readonly PullRequestService _pullRequestService;
    private readonly ScoreService _scoreService;
    private readonly UserService _userService;

    public AssignmentEventProcessorService(
        GradeManagementDbContext gradeManagementDbContext, AssignmentService assignmentService,
        AssignmentLogService assignmentLogService,
        ExerciseService exerciseService, StudentService studentService,
        PullRequestService pullRequestService, ScoreService scoreService, UserService userService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _assignmentService = assignmentService;
        _exerciseService = exerciseService;
        _studentService = studentService;
        _pullRequestService = pullRequestService;
        _scoreService = scoreService;
        _userService = userService;
        _assignmentLogService = assignmentLogService;
    }

    public string GetRepositoryNameFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/');
        return
            string.Join("/",
                segments.Skip(
                    2)); // Skip the first 2 segments: "", "{subjectcode}", Join with "/" is needed to account for the possibility of the repository name containing slashes
    }

    public async Task ConsumeAssignmentAcceptedEventAsync(AssignmentAccepted assignmentAccepted)
    {
        var repositoryName = GetRepositoryNameFromUrl(assignmentAccepted.GitHubRepositoryUrl);
        var exercise = await _exerciseService.GetExerciseModelByGitHubRepoNameAsync(repositoryName);
        var studentGitHubId = repositoryName.Remove(0, (exercise.GithubPrefix + "-").Length);
        var student = await _studentService.GetOrCreateStudentByGitHubIdAsync(studentGitHubId);
        var assignment = new Assignment()
        {
            GithubRepoName = repositoryName,
            GithubRepoUrl = assignmentAccepted.GitHubRepositoryUrl,
            StudentId = student.Id,
            ExerciseId = exercise.Id
        };
        await _assignmentService.CreateAsync(assignment);

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.AssignmentAccepted,
            Description = $"Assignment for exercise {exercise.Name} accepted by student {studentGitHubId}",
            AssignmentId = assignment.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }

    public async Task ConsumePullRequestOpenedEventAsync(PullRequestOpened pullRequestOpened)
    {
        var repositoryName = GetRepositoryNameFromUrl(pullRequestOpened.GitHubRepositoryUrl);
        var assignment = await _assignmentService.GetAssignmentModelByGitHubRepoNameAsync(repositoryName);
        var pullRequest = new PullRequest()
        {
            Url = pullRequestOpened.PullRequestUrl,
            OpeningDate = pullRequestOpened.OpeningDate,
            Status = PullRequestStatus.Open,
            BranchName = pullRequestOpened.BranchName,
            AssignmentId = assignment.Id
        };
        await _pullRequestService.CreateAsync(pullRequest);

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.PullRequestOpened,
            Description = $"Pull request opened for assignment {assignment.GithubRepoUrl} with id {assignment.Id}",
            AssignmentId = assignment.Id,
            PullRequestId = pullRequest.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }

    public async Task ConsumeCiEvaluationCompletedEventAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(ciEvaluationCompleted.PullRequestUrl);

        var repositoryName = GetRepositoryNameFromUrl(ciEvaluationCompleted.GitHubRepositoryUrl);
        var exercise = await _exerciseService.GetExerciseModelByGitHubRepoNameAsync(repositoryName);
        var studentGitHubId = repositoryName.Remove(0, (exercise.GithubPrefix + "-").Length);
        var student = await _studentService.GetStudentModelByGitHubIdAsync(studentGitHubId);
        var assignment = await _assignmentService.GetAssignmentModelByGitHubRepoNameAsync(repositoryName);

        if (student.NeptunCode.IsNullOrEmpty())
        {
            await _studentService.DeleteAsync(student.Id);
            student = await _studentService.GetStudentModelByNeptunAsync(ciEvaluationCompleted.StudentNeptun);
            student.GithubId = studentGitHubId;
            assignment.StudentId = student.Id;
            await _gradeManagementDbContext.SaveChangesAsync();
        }

        foreach (var scoreEvent in ciEvaluationCompleted.Scores)
        {
            await _scoreService.CreateScoreBasedOnEventScoreAsync(scoreEvent, pullRequest.Id);
        }

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.CiEvaluationCompleted,
            Description = $"CI evaluation completed for assignment {assignment.GithubRepoUrl} with id {assignment.Id}",
            AssignmentId = assignment.Id,
            PullRequestId = pullRequest.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }

    public async Task ConsumeTeacherAssignedEventAsync(TeacherAssigned teacherAssigned)
    {
        var repositoryName = GetRepositoryNameFromUrl(teacherAssigned.GitHubRepositoryUrl);
        var assignment = await _assignmentService.GetAssignmentModelByGitHubRepoNameAsync(repositoryName);
        var teacher = await _userService.GetModelByGitHubIdAsync(teacherAssigned.TeacherGitHubId);
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(teacherAssigned.PullRequestUrl);
        pullRequest.TeacherId = teacher.Id;
        await _gradeManagementDbContext.SaveChangesAsync();

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.TeacherAssigned,
            Description = $"Teacher {teacher.GithubId} assigned to pull request {pullRequest.Url} with id {pullRequest.Id}",
            AssignmentId = assignment.Id,
            PullRequestId = pullRequest.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }

    public async Task ConsumeAssignmentGradedByTeacherEventAsync(AssignmentGradedByTeacher assignmentGradedByTeacher)
    {
        var repositoryName = GetRepositoryNameFromUrl(assignmentGradedByTeacher.GitHubRepositoryUrl);
        var assignment = await _assignmentService.GetAssignmentModelByGitHubRepoNameAsync(repositoryName);
        var teacher = await _userService.GetModelByGitHubIdAsync(assignmentGradedByTeacher.TeacherGitHubId);
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(assignmentGradedByTeacher.PullRequestUrl);

        if (assignmentGradedByTeacher.Scores.IsNullOrEmpty())
        {
            var latestScores = await _pullRequestService.GetLatestUnapprovedScoremodelsByIdAsync(pullRequest.Id);
            foreach (var scoreEntity in latestScores)
            {
                scoreEntity.IsApproved = true;
                scoreEntity.TeacherId = teacher.Id;
            }

            await _gradeManagementDbContext.SaveChangesAsync();
        }
        else
        {
            foreach (var eventScore in assignmentGradedByTeacher.Scores)
            {
                await _scoreService.CreateOrApprovePointsFromTeacherInput(eventScore, pullRequest.Id, teacher.Id);
            }
        }

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.AssignmentGradedByTeacher,
            Description = $"Assignment {assignment.GithubRepoUrl} with id {assignment.Id} graded by teacher {teacher.GithubId}",
            AssignmentId = assignment.Id,
            PullRequestId = pullRequest.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }

    public async Task ConsumePullRequestStatusChangedEventAsync(PullRequestStatusChanged pullRequestStatusChanged)
    {
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(pullRequestStatusChanged.PullRequestUrl);
        pullRequest.Status = pullRequestStatusChanged.pullRequestStatus;
        await _gradeManagementDbContext.SaveChangesAsync();

        var assignmentLog = new AssignmentLog()
        {
            EventType = EventType.PullRequestStatusChanged,
            Description = $"Pull request {pullRequest.Url} with id {pullRequest.Id} status changed to {pullRequest.Status}",
            AssignmentId = pullRequest.AssignmentId,
            PullRequestId = pullRequest.Id
        };
        await _assignmentLogService.CreateAsync(assignmentLog);
    }
}
