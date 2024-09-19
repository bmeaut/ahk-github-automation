using GradeManagement.Data;
using GradeManagement.Data.Models;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class ScoreTypeService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;

    public ScoreTypeService(GradeManagementDbContext gradeManagementDbContext)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
    }

    public async Task<ScoreType> GetOrCreateScoreTypeByTypeStringAsync(string eventScoreScoreType)
    {
        var scoreType =
            await _gradeManagementDbContext.ScoreType.SingleOrDefaultAsync(st => st.Type == eventScoreScoreType);
        if (scoreType == null)
        {
            scoreType = new ScoreType { Type = eventScoreScoreType };
            _gradeManagementDbContext.ScoreType.Add(scoreType);
            await _gradeManagementDbContext.SaveChangesAsync();
        }

        return scoreType;
    }
}
