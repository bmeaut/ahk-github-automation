using AutoMapper;

using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Shared.Dtos.AssignmentEvents;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class ScoreService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public ScoreService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task CreateScoreBasedOnEventScoreAsync(EventScore eventScore, long pullRequestId)
    {
        var scoreEntity = new Score
        {
            Value = eventScore.Value,
            IsApproved = false,
            CreatedDate = eventScore.CreatedDate,
            ScoreType = await GetOrCreateScoreTypeByTypeStringAsync(eventScore.ScoreType),
            PullRequestId = pullRequestId
        };
        _gradeManagementDbContext.Score.Add(scoreEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task CreateOrApprovePointsFromTeacherInput(EventScore eventScore, long pullRequestId, long teacherId)
    {
        var scoreEntity = await GetLatestModelByEventScoreAsync(eventScore);
        if (scoreEntity == null)
        {
            scoreEntity = new Score
            {
                Value = eventScore.Value,
                IsApproved = true,
                CreatedDate = eventScore.CreatedDate,
                ScoreType = await GetOrCreateScoreTypeByTypeStringAsync(eventScore.ScoreType),
                PullRequestId = pullRequestId,
                TeacherId = teacherId
            };
            _gradeManagementDbContext.Score.Add(scoreEntity);
        }
        else
        {
            scoreEntity.IsApproved = true;
            scoreEntity.TeacherId = teacherId;
        }


        await _gradeManagementDbContext.SaveChangesAsync();
    }

    private async Task<ScoreType> GetOrCreateScoreTypeByTypeStringAsync(string eventScoreScoreType)
    {
        var scoreType = await _gradeManagementDbContext.ScoreType.SingleOrDefaultAsync(st => st.Type == eventScoreScoreType);
        if (scoreType == null)
        {
            scoreType = new ScoreType
            {
                Type = eventScoreScoreType
            };
            _gradeManagementDbContext.ScoreType.Add(scoreType);
            await _gradeManagementDbContext.SaveChangesAsync();
        }

        return scoreType;
    }

    public async Task<Score?> GetLatestModelByEventScoreAsync(EventScore eventScore)
    {
        return await _gradeManagementDbContext.Score
            .Include(s => s.ScoreType)
            .OrderByDescending(s=>s.CreatedDate)
            .FirstOrDefaultAsync(s => s.Value == eventScore.Value && s.ScoreType.Type == eventScore.ScoreType);
    }
}
