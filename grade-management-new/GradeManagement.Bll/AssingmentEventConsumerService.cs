using AutoMapper;

using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.IdentityModel.Tokens;

namespace GradeManagement.Bll;

public class AssingmentEventConsumerService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly AssignmentService _assignmentService;
    private readonly ExerciseService _exerciseService;
    private readonly StudentService _studentService;
    private readonly PullRequestService _pullRequestService;
    private readonly ScoreService _scoreService;
    private readonly UserService _userService;

    public AssingmentEventConsumerService(
        GradeManagementDbContext gradeManagementDbContext, IMapper mapper, AssignmentService assignmentService,
        ExerciseService exerciseService, StudentService studentService,
        PullRequestService pullRequestService, ScoreService scoreService, UserService userService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _assignmentService = assignmentService;
        _exerciseService = exerciseService;
        _studentService = studentService;
        _pullRequestService = pullRequestService;
        _scoreService = scoreService;
        _userService = userService;
    }

    public string GetRepositoryNameFromUrl(string url)
    {
        var uri = new Uri(url);
        var segments = uri.AbsolutePath.Split('/');
        return string.Join("/", segments.Skip(2)); // Skip the first 2 segments: "", "{subjectcode}", Join with "/" is needed to account for the possibility of the repository name containing slashes
    }

    public async Task ConsumeAssignmentAcceptedEventAsync(AssignmentAccepted assignmentAccepted)
    {
        var repositoryName = GetRepositoryNameFromUrl(assignmentAccepted.GitHubRepositoryUrl);
        var exercise = await _exerciseService.GetExerciseModelByGitHubRepoNameAsync(repositoryName);
        var studentGitHubId = repositoryName.Remove(0, (exercise.GithubPrefix + "-").Length);
        var student = await _studentService.GetOrCreateStudentByGitHubIdAsync(studentGitHubId);
        var assignment = new Assignment()
        {
            GithubRepoName = repositoryName, StudentId = student.Id, ExerciseId = exercise.Id
        };
        await _assignmentService.CreateAsync(assignment);
    }

    public async Task ConsumePullRequestOpenedEvent(PullRequestOpened pullRequestOpened)
    {
        var repositoryName = GetRepositoryNameFromUrl(pullRequestOpened.GitHubRepositoryUrl);
        var assignment = await _assignmentService.GetAssignmentModelByGitHubRepoNameAsync(repositoryName);
        var pullRequest = new PullRequest()
        {
            Url = pullRequestOpened.PullRequestUrl,
            OpeningDate = pullRequestOpened.OpeningDate,
            IsClosed = false,
            BranchName = pullRequestOpened.BranchName,
            AssignmentId = assignment.Id
        };
        await _pullRequestService.CreateAsync(pullRequest);
    }

    public async Task ConsumeCiEvaluationCompletedEvent(CiEvaluationCompleted ciEvaluationCompleted)
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
    }

    public async Task ConsumeTeacherAssignedEvent(TeacherAssigned teacherAssigned)
    {
        var teacher = await _userService.GetModelByGitHubIdAsync(teacherAssigned.TeacherGitHubId);
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(teacherAssigned.PullRequestUrl);
        pullRequest.TeacherId = teacher.Id;
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task ConsumeAssignmentGradedByTeacherEvent(AssignmentGradedByTeacher assignmentGradedByTeacher)
    {
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
    }

    public async Task ConsumePullRequestClosedEvent(PullRequestClosed pullRequestClosed)
    {
        var pullRequest = await _pullRequestService.GetModelByUrlAsync(pullRequestClosed.PullRequestUrl);
        pullRequest.IsClosed = true;
        await _gradeManagementDbContext.SaveChangesAsync();
    }
}
