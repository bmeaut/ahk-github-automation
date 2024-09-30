using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Shared.Dtos.AssignmentEvents;
using GradeManagement.Shared.Enums;

using Microsoft.IdentityModel.Tokens;

using Assignment = GradeManagement.Shared.Dtos.Assignment;
using PullRequest = GradeManagement.Shared.Dtos.PullRequest;

namespace GradeManagement.Bll.Services;

public class AssignmentEventProcessorService(
    GradeManagementDbContext gradeManagementDbContext,
    AssignmentService assignmentService,
    AssignmentLogService assignmentLogService,
    ExerciseService exerciseService,
    StudentService studentService,
    PullRequestService pullRequestService,
    ScoreService scoreService,
    UserService userService,
    SubjectService subjectService)
{
    private string GetRepositoryNameFromUrl(string url)
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
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var repositoryName = GetRepositoryNameFromUrl(assignmentAccepted.GitHubRepositoryUrl);
            var exercise = await exerciseService.GetExerciseModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var studentGitHubId = repositoryName.Remove(0, (exercise.GithubPrefix + "-").Length);
            var student = await studentService.GetOrCreateStudentByGitHubIdAsync(studentGitHubId);
            var assignment = new Assignment()
            {
                GithubRepoName = repositoryName,
                GithubRepoUrl = assignmentAccepted.GitHubRepositoryUrl,
                StudentId = student.Id,
                ExerciseId = exercise.Id
            };
            assignment = await assignmentService.CreateAsync(assignment, exercise.SubjectId);

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.AssignmentAccepted,
                Description = $"Assignment for exercise {exercise.Name} accepted by student {studentGitHubId}",
                AssignmentId = assignment.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConsumePullRequestOpenedEventAsync(PullRequestOpened pullRequestOpened)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var repositoryName = GetRepositoryNameFromUrl(pullRequestOpened.GitHubRepositoryUrl);
            var assignment = await assignmentService.GetAssignmentModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var pullRequest = new PullRequest()
            {
                Url = pullRequestOpened.PullRequestUrl,
                OpeningDate = pullRequestOpened.OpeningDate,
                Status = PullRequestStatus.Open,
                BranchName = pullRequestOpened.BranchName,
                AssignmentId = assignment.Id
            };
            pullRequest = await pullRequestService.CreateAsync(pullRequest, assignment.SubjectId);

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.PullRequestOpened,
                Description =
                    $"Pull request opened for assignment {assignment.GithubRepoUrl} with id {assignment.Id}",
                AssignmentId = assignment.Id,
                PullRequestId = pullRequest.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConsumeCiEvaluationCompletedEventAsync(CiEvaluationCompleted ciEvaluationCompleted)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var pullRequest = await pullRequestService.GetModelByUrlWithoutQfAsync(ciEvaluationCompleted.PullRequestUrl);
            var repositoryName = GetRepositoryNameFromUrl(ciEvaluationCompleted.GitHubRepositoryUrl);
            var exercise = await exerciseService.GetExerciseModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var studentGitHubId = repositoryName.Remove(0, (exercise.GithubPrefix + "-").Length);
            var student = await studentService.GetStudentModelByGitHubIdAsync(studentGitHubId);
            var assignment = await assignmentService.GetAssignmentModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var subject = await subjectService.GetModelByIdWithoutQfAsync(exercise.SubjectId);

            if(ciEvaluationCompleted.CiApiKey != subject.CiApiKey)
            {
                throw new SecurityTokenException("Invalid API key");
            }

            if (student.NeptunCode.IsNullOrEmpty())
            {
                var newStudent =
                    await studentService.GetStudentModelByNeptunAsync(ciEvaluationCompleted.StudentNeptun);
                newStudent.GithubId = studentGitHubId;
                await assignmentService.ChangeStudentIdOnAllAssignmentsWithoutQfAsync(student.Id, newStudent.Id);
                await gradeManagementDbContext.SaveChangesAsync();
                await studentService.DeleteAsync(student.Id);
                student = newStudent;
            }

            foreach (var scoreEvent in ciEvaluationCompleted.Scores)
            {
                await scoreService.CreateScoreBasedOnEventScoreAsync(scoreEvent, pullRequest.Id, pullRequest.SubjectId);
            }

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.CiEvaluationCompleted,
                Description =
                    $"CI evaluation completed for assignment {assignment.GithubRepoUrl} with id {assignment.Id}",
                AssignmentId = assignment.Id,
                PullRequestId = pullRequest.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConsumeTeacherAssignedEventAsync(TeacherAssigned teacherAssigned)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var repositoryName = GetRepositoryNameFromUrl(teacherAssigned.GitHubRepositoryUrl);
            var assignment = await assignmentService.GetAssignmentModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var teacher = await userService.GetModelByGitHubIdAsync(teacherAssigned.TeacherGitHubId);
            var pullRequest = await pullRequestService.GetModelByUrlWithoutQfAsync(teacherAssigned.PullRequestUrl);
            pullRequest.TeacherId = teacher.Id;
            await gradeManagementDbContext.SaveChangesAsync();

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.TeacherAssigned,
                Description =
                    $"Teacher {teacher.GithubId} assigned to pull request {pullRequest.Url} with id {pullRequest.Id}",
                AssignmentId = assignment.Id,
                PullRequestId = pullRequest.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConsumeAssignmentGradedByTeacherEventAsync(AssignmentGradedByTeacher assignmentGradedByTeacher)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var repositoryName = GetRepositoryNameFromUrl(assignmentGradedByTeacher.GitHubRepositoryUrl);
            var assignment = await assignmentService.GetAssignmentModelByGitHubRepoNameWithoutQfAsync(repositoryName);
            var teacher = await userService.GetModelByGitHubIdAsync(assignmentGradedByTeacher.TeacherGitHubId);
            var pullRequest = await pullRequestService.GetModelByUrlWithoutQfAsync(assignmentGradedByTeacher.PullRequestUrl);

            if (assignmentGradedByTeacher.Scores.IsNullOrEmpty())
            {
                var latestScores = await pullRequestService.GetLatestUnapprovedScoreModelsWithoutQfByIdAsync(pullRequest.Id);
                foreach (var scoreEntity in latestScores)
                {
                    scoreEntity.IsApproved = true;
                    scoreEntity.TeacherId = teacher.Id;
                }

                await gradeManagementDbContext.SaveChangesAsync();
            }
            else
            {
                foreach (var eventScore in assignmentGradedByTeacher.Scores)
                {
                    await scoreService.CreateOrApprovePointsFromTeacherInputWithoutQfAsync(eventScore, pullRequest.Id, teacher.Id, pullRequest.SubjectId);
                }
            }

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.AssignmentGradedByTeacher,
                Description =
                    $"Assignment {assignment.GithubRepoUrl} with id {assignment.Id} graded by teacher {teacher.GithubId}",
                AssignmentId = assignment.Id,
                PullRequestId = pullRequest.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task ConsumePullRequestStatusChangedEventAsync(PullRequestStatusChanged pullRequestStatusChanged)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            var pullRequest = await pullRequestService.GetModelByUrlWithoutQfAsync(pullRequestStatusChanged.PullRequestUrl);
            pullRequest.Status = pullRequestStatusChanged.pullRequestStatus;
            await gradeManagementDbContext.SaveChangesAsync();

            var assignmentLog = new AssignmentLog()
            {
                EventType = EventType.PullRequestStatusChanged,
                Description =
                    $"Pull request {pullRequest.Url} with id {pullRequest.Id} status changed to {pullRequest.Status}",
                AssignmentId = pullRequest.AssignmentId,
                PullRequestId = pullRequest.Id
            };
            await assignmentLogService.CreateAsync(assignmentLog);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
