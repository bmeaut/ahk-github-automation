using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Data;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class PullRequestService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
{
    public async Task<PullRequest> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.PullRequest
            .ProjectTo<PullRequest>(mapper.ConfigurationProvider)
            .SingleEntityAsync(p => p.Id == id, id);
    }

    public async Task<Data.Models.PullRequest> GetModelByUrlWithoutQfAsync(string pullRequestUrl)
    {
        return await gradeManagementDbContext.PullRequest
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(p => p.Url == pullRequestUrl, 0);
    }

    public async Task<PullRequest> CreateAsync(PullRequest pullRequest, long subjectId)
    {
        var pullRequestEntity = new Data.Models.PullRequest()
        {
            Url = pullRequest.Url,
            OpeningDate = pullRequest.OpeningDate,
            Status = pullRequest.Status,
            BranchName = pullRequest.BranchName,
            AssignmentId = pullRequest.AssignmentId,
            SubjectId = subjectId
        };

        gradeManagementDbContext.PullRequest.Add(pullRequestEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(pullRequestEntity.Id);
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

    public async Task<List<Data.Models.Score>> GetLatestUnapprovedScoreModelsWithoutQfByIdAsync(long id)
    {
        return await gradeManagementDbContext.Score
            .IgnoreQueryFiltersButNotIsDeleted()
            .Where(s => s.PullRequestId == id && !s.IsApproved)
            .Include(s=>s.ScoreType)
            .GroupBy(s => s.ScoreType)
            .Select(g => g.OrderByDescending(s => s.CreatedDate).FirstOrDefault())
            .ToListAsync();
    }

    public async Task<List<Data.Models.Score>> GetApprovedScoreModelsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Score
            .Where(s => s.PullRequestId == id && s.IsApproved)
            .Include(s => s.ScoreType)
            .ToListAsync();
    }
}
