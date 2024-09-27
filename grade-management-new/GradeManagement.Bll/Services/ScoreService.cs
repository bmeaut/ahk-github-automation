using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class ScoreService(GradeManagementDbContext gradeManagementDbContext, ScoreTypeService scoreTypeService)
{
    public async Task CreateScoreBasedOnEventScoreAsync(EventScore eventScore, long pullRequestId, long subjectId)
    {
        var scoreEntity = new Score
        {
            Value = eventScore.Value,
            IsApproved = false,
            CreatedDate = eventScore.CreatedDate,
            ScoreType = await scoreTypeService.GetOrCreateScoreTypeByTypeStringAsync(eventScore.ScoreType),
            PullRequestId = pullRequestId
        };
        gradeManagementDbContext.Score.Add(scoreEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task CreateOrApprovePointsFromTeacherInputWithoutQfAsync(
        EventScore eventScore, long pullRequestId, long teacherId, long subjectId)
    {
        var scoreEntity = await GetLatestModelByEventScoreWithoutQfAsync(eventScore);
        if (scoreEntity == null)
        {
            scoreEntity = new Score
            {
                Value = eventScore.Value,
                IsApproved = true,
                CreatedDate = eventScore.CreatedDate,
                ScoreType = await scoreTypeService.GetOrCreateScoreTypeByTypeStringAsync(eventScore.ScoreType),
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
    }

    private async Task<Score?> GetLatestModelByEventScoreWithoutQfAsync(EventScore eventScore)
    {
        return await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Include(s => s.ScoreType)
            .OrderByDescending(s => s.CreatedDate)
            .FirstOrDefaultAsync(s => s.Value == eventScore.Value && s.ScoreType.Type == eventScore.ScoreType);
    }
}
