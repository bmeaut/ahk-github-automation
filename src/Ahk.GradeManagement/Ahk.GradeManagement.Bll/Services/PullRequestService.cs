using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Extensions;
using Ahk.GradeManagement.Shared.Dtos;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using Microsoft.EntityFrameworkCore;

namespace Ahk.GradeManagement.Bll.Services;

public class PullRequestService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
{
    public async Task<PullRequest> GetByIdWithoutQfAsync(long id)
    {
        return await gradeManagementDbContext.PullRequest
            .IgnoreQueryFiltersButNotIsDeleted()
            .ProjectTo<PullRequest>(mapper.ConfigurationProvider)
            .SingleEntityAsync(p => p.Id == id, id);
    }

    public async Task<Dal.Entities.PullRequest?> GetEntityByGitHubIdWithoutQfAsync(long gitHubId)
    {
        return await gradeManagementDbContext.PullRequest
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleOrDefaultAsync(p => p.GitHubId == gitHubId);
    }

    public async Task<PullRequest> CreateWithoutQfAsync(PullRequest pullRequest, long subjectId)
    {
        var pullRequestEntity = new Dal.Entities.PullRequest()
        {
            GitHubId = pullRequest.GitHubId,
            Url = pullRequest.Url,
            OpeningDate = pullRequest.OpeningDate,
            Status = pullRequest.Status,
            BranchName = pullRequest.BranchName,
            AssignmentId = pullRequest.AssignmentId,
            SubjectId = subjectId
        };

        gradeManagementDbContext.PullRequest.Add(pullRequestEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdWithoutQfAsync(pullRequestEntity.Id);
    }

    public async Task<IEnumerable<Score>> GetAllScoresByIdSortedByDateDescendingAsync(long id)
    {
        return await gradeManagementDbContext.Score
            .Where(s => s.PullRequestId == id)
            .Include(s => s.ScoreType)
            .OrderByDescending(s => s.CreatedDate)
            .ProjectTo<Score>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Dal.Entities.Score>> GetLatestUnapprovedScoreModelsWithoutQfByIdAsync(long id)
    {
        return await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Where(s => s.PullRequestId == id && !s.IsApproved)
            .Include(s => s.ScoreType)
            .GroupBy(s => s.ScoreType)
            .Select(g => g.OrderByDescending(s => s.CreatedDate).FirstOrDefault())
            .ToListAsync();
    }
}
