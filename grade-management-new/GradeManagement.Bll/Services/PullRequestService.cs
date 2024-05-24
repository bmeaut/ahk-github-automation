using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class PullRequestService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public PullRequestService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<PullRequest> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.PullRequest
            .ProjectTo<PullRequest>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(p => p.Id == id, id);
    }

    public async Task<Data.Models.PullRequest> GetModelByUrlAsync(string pullRequestUrl)
    {
        return await _gradeManagementDbContext.PullRequest
            .SingleEntityAsync(p => p.Url == pullRequestUrl, 0);
    }

    public async Task<PullRequest> CreateAsync(PullRequest pullRequest)
    {
        var pullRequestEntity = new Data.Models.PullRequest()
        {
            Url = pullRequest.Url,
            OpeningDate = pullRequest.OpeningDate,
            Status = pullRequest.Status,
            BranchName = pullRequest.BranchName,
            AssignmentId = pullRequest.AssignmentId,
        };

        _gradeManagementDbContext.PullRequest.Add(pullRequestEntity);
        await _gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(pullRequestEntity.Id);
    }

    public async Task<PullRequest> UpdateAsync(long id, PullRequest pullRequest)
    {
        if (pullRequest.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var pullRequestEntity = await _gradeManagementDbContext.PullRequest
            .SingleEntityAsync(p => p.Id == id, id);

        pullRequestEntity.Url = pullRequest.Url;
        pullRequestEntity.OpeningDate = pullRequest.OpeningDate;
        pullRequestEntity.Status = pullRequest.Status;
        pullRequestEntity.BranchName = pullRequest.BranchName;
        pullRequestEntity.AssignmentId = pullRequest.AssignmentId;

        await _gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(pullRequestEntity.Id);
    }

    public async Task<IEnumerable<Score>> GetAllScoresByIdSortedByDateDescendingAsync(long id)
    {
        return await _gradeManagementDbContext.Score
            .Where(s => s.PullRequestId == id)
            .Include(s => s.ScoreType)
            .OrderByDescending(s => s.CreatedDate)
            .ProjectTo<Score>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Data.Models.Score>> GetLatestUnapprovedScoremodelsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Score
            .Where(s => s.PullRequestId == id && !s.IsApproved)
            .Include(s=>s.ScoreType)
            .GroupBy(s => s.ScoreType)
            .Select(g => g.OrderByDescending(s => s.CreatedDate).FirstOrDefault())
            .ToListAsync();
    }

    public async Task<List<Data.Models.Score>> GetApprovedScoreModelsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Score
            .Where(s => s.PullRequestId == id && s.IsApproved)
            .Include(s => s.ScoreType)
            .ToListAsync();
    }
}
