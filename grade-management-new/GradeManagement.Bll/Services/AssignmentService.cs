using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Enums;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class AssignmentService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper /*, ExerciseService exerciseService*/)
    : IQueryServiceBase<Assignment>
{
    public async Task<IEnumerable<Assignment>> GetAllAsync()
    {
        return await gradeManagementDbContext.Assignment
            .ProjectTo<Assignment>(mapper.ConfigurationProvider)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    public async Task<Assignment> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Assignment
            .ProjectTo<Assignment>(mapper.ConfigurationProvider)
            .SingleEntityAsync(a => a.Id == id, id);
    }

    public async Task<Assignment> CreateAsync(Assignment requestDto, long subjectId)
    {
        var assignmentEntity = new Data.Models.Assignment()
        {
            Id = requestDto.Id,
            GithubRepoName = requestDto.GithubRepoName,
            GithubRepoUrl = requestDto.GithubRepoUrl,
            StudentId = requestDto.StudentId,
            ExerciseId = requestDto.ExerciseId,
            SubjectId = subjectId
        };
        gradeManagementDbContext.Assignment.Add(assignmentEntity);
        await gradeManagementDbContext.SaveChangesAsync();
        return mapper.Map<Assignment>(assignmentEntity);
    }

    public async Task<IEnumerable<PullRequest>> GetAllPullRequestsByIdAsync(long id)
    {
        return await gradeManagementDbContext.PullRequest
            .ProjectTo<PullRequest>(mapper.ConfigurationProvider)
            .Where(p => p.AssignmentId == id)
            .ToListAsync();
    }

    public async Task<Data.Models.PullRequest?> GetMergedPullRequestModelByIdAsync(long id)
    {
        return await gradeManagementDbContext.PullRequest
            .SingleOrDefaultAsync(pr => pr.AssignmentId == id && pr.Status == PullRequestStatus.Merged);
    }

    public async Task<Data.Models.Assignment> GetAssignmentModelByGitHubRepoNameWithoutQfAsync(string githubRepoName)
    {
        return await gradeManagementDbContext.Assignment
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(a => githubRepoName == a.GithubRepoName, 0);
    }

    public async Task ChangeStudentIdOnAllAssignmentsWithoutQfAsync(long studentIdFrom, long studentIdTo)
    {
        var assignments = await gradeManagementDbContext.Assignment
            .IgnoreQueryFiltersButNotIsDeleted()
            .Where(a => a.StudentId == studentIdFrom)
            .ToListAsync();
        foreach (var assignment in assignments)
        {
            assignment.StudentId = studentIdTo;
        }

        await gradeManagementDbContext.SaveChangesAsync();
    }
}
