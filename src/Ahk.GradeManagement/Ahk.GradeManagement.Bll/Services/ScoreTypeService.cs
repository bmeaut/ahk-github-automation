using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Entities;

using Microsoft.EntityFrameworkCore;

namespace Ahk.GradeManagement.Bll.Services;

public class ScoreTypeService(GradeManagementDbContext gradeManagementDbContext)
{
    public async Task<ScoreType> GetOrCreateScoreTypeByTypeStringAsync(string eventScoreScoreType)
    {
        var scoreType =
            await gradeManagementDbContext.ScoreType.SingleOrDefaultAsync(st => st.Type == eventScoreScoreType);

        if (scoreType != null) return scoreType;

        scoreType = new ScoreType { Type = eventScoreScoreType };
        gradeManagementDbContext.ScoreType.Add(scoreType);
        await gradeManagementDbContext.SaveChangesAsync();

        return scoreType;
    }
}
