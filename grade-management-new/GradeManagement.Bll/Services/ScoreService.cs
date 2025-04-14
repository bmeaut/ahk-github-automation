using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.Moodle;
using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class ScoreService(
    GradeManagementDbContext gradeManagementDbContext,
    MoodleIntegrationService moodleIntegrationService,
    ExerciseService exerciseService)
{
    public async Task CreateScoreBasedOnOrderAsync(int order, double score, long pullRequestId, long subjectId,
        long exerciseId)
    {
        var scoreEntity = new Score
        {
            Value = score,
            IsApproved = false,
            CreatedDate = DateTimeOffset.Now,
            ScoreType = await exerciseService.GetScoreTypeByOrderAndExerciseIdAsync(order, exerciseId),
            PullRequestId = pullRequestId,
            SubjectId = subjectId
        };
        gradeManagementDbContext.Score.Add(scoreEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task ApproveScoreAsync(long scoreId, long teacherId)
    {
        var score = await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Include(s => s.PullRequest)
            .ThenInclude(pr => pr.Assignment).ThenInclude(a => a.Exercise)
            .Include(s => s.PullRequest)
            .ThenInclude(pr => pr.Assignment).ThenInclude(a => a.Student)
            .Include(s => s.ScoreType)
            .SingleEntityAsync(s => s.Id == scoreId, scoreId);

        score.IsApproved = true;
        score.TeacherId = teacherId;

        gradeManagementDbContext.SaveChangesAsync();
        moodleIntegrationService.UploadScore(score);
    }

    public async Task CreateOrApprovePointsFromTeacherInputWithoutQfAsync(
        int order, double score, long pullRequestId, long teacherId, long subjectId, long exerciseId)
    {
        var scoreType = await exerciseService.GetScoreTypeByOrderAndExerciseIdAsync(order, exerciseId);
        var scoreEntity = await GetLatestModelByScoreValueAndTypeWithoutQfAsync(score, scoreType);
        if (scoreEntity == null)
        {
            scoreEntity = new Score
            {
                Value = score,
                IsApproved = true,
                CreatedDate = DateTimeOffset.Now,
                ScoreType = scoreType,
                PullRequestId = pullRequestId,
                TeacherId = teacherId,
                SubjectId = subjectId
            };
            gradeManagementDbContext.Score.Add(scoreEntity);
        }
        else
        {
            scoreEntity.IsApproved = true;
            scoreEntity.TeacherId = teacherId;
        }


        await gradeManagementDbContext.SaveChangesAsync();

        var scoreToUpload = await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Include(s => s.PullRequest)
            .ThenInclude(pr => pr.Assignment).ThenInclude(a => a.Exercise)
            .Include(s => s.PullRequest)
            .ThenInclude(pr => pr.Assignment).ThenInclude(a => a.Student)
            .Include(s => s.ScoreType)
            .SingleEntityAsync(s => s.Id == scoreEntity.Id, scoreEntity.Id);

        moodleIntegrationService.UploadScore(scoreToUpload);
    }

    private async Task<Score?> GetLatestModelByScoreValueAndTypeWithoutQfAsync(double score, ScoreType scoreType)
    {
        return await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Include(s => s.ScoreType)
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefaultAsync(s => Math.Abs(s.Value - score) < 0.001 && s.ScoreType.Id == scoreType.Id);
    }
}
