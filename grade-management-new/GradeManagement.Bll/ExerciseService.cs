using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class ExerciseService : ICrudServiceBase<Exercise>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public ExerciseService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Exercise>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Exercise
            .ProjectTo<Exercise>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Exercise> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Exercise
            .ProjectTo<Exercise>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(e => e.Id == id, id);
    }

    public async Task<Exercise> CreateAsync(Exercise requestDto)
    {
        var exerciseEntity = new Data.Models.Exercise()
        {
            Name = requestDto.Name,
            GithubPrefix = requestDto.GithubPrefix,
            dueDate = requestDto.dueDate,
            CourseId = requestDto.CourseId
        };
        _gradeManagementDbContext.Exercise.Add(exerciseEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(exerciseEntity.Id);
    }

    public async Task<Exercise> UpdateAsync(long id, Exercise requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var exerciseEntity = await _gradeManagementDbContext.Exercise
            .SingleEntityAsync(e => e.Id == id, id);

        exerciseEntity.Name = requestDto.Name;
        exerciseEntity.GithubPrefix = requestDto.GithubPrefix;
        exerciseEntity.dueDate = requestDto.dueDate;
        exerciseEntity.CourseId = requestDto.CourseId;

        await _gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(exerciseEntity.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var exerciseEntity = await _gradeManagementDbContext.Exercise.SingleEntityAsync(e => e.Id == id, id);
        _gradeManagementDbContext.Exercise.Remove(exerciseEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }


    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Assignment
            .Where(a => a.ExerciseId == id)
            .ProjectTo<Assignment>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Data.Models.Exercise> GetExerciseByGitHubRepoNameAsync(string githubRepoName)
    {
        return await _gradeManagementDbContext.Exercise
            .SingleEntityAsync(e => githubRepoName.StartsWith(e.GithubPrefix), 0);
    }
}
