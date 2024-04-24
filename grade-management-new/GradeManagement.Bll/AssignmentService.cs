using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class AssignmentService : IQueryServiceBase<Assignment>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public AssignmentService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Assignment>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Assignment
            .ProjectTo<Assignment>(_mapper.ConfigurationProvider)
            .OrderBy(a => a.Id)
            .ToListAsync();
    }

    public async Task<Assignment> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Assignment
            .ProjectTo<Assignment>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(a => a.Id == id, id);
    }

    public async Task<Assignment> CreateAsync(Assignment requestDto)
    {
        var assignmentEntity = new Data.Models.Assignment()
        {
            Id = requestDto.Id,
            GithubRepoName = requestDto.GithubRepoName,
            StudentId = requestDto.StudentId,
            ExerciseId = requestDto.ExerciseId
        };
        _gradeManagementDbContext.Assignment.Add(assignmentEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Assignment>(assignmentEntity);
    }

    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Score
            .ProjectTo<Score>(_mapper.ConfigurationProvider)
            .Where(s => s.AssignmentId == id)
            .Include(s=>s.ScoreType)
            .ToListAsync();
    }

    public async Task<IEnumerable<PullRequest>> GetAllPullRequestsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.PullRequest
            .ProjectTo<PullRequest>(_mapper.ConfigurationProvider)
            .Where(p => p.AssignmentId == id)
            .ToListAsync();
    }
}
