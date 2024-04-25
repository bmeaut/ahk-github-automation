﻿using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

using Assignment = GradeManagement.Data.Models.Assignment;

namespace GradeManagement.Bll;

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
            IsClosed = pullRequest.IsClosed,
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
        pullRequestEntity.IsClosed = pullRequest.IsClosed;
        pullRequestEntity.BranchName = pullRequest.BranchName;
        pullRequestEntity.AssignmentId = pullRequest.AssignmentId;

        await _gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(pullRequestEntity.Id);
    }

    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Score
            .ProjectTo<Score>(_mapper.ConfigurationProvider)
            .Where(s => s.PullRequesId == id)
            .Include(s => s.ScoreType)
            .ToListAsync();
    }
}
