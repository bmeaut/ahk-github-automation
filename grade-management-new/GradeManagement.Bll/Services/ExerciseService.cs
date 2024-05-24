using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using CsvHelper;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using ValidationException = AutSoft.Common.Exceptions.ValidationException;

namespace GradeManagement.Bll.Services;

public class ExerciseService : ICrudServiceBase<Exercise>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly PullRequestService _pullRequestService;
    private readonly AssignmentService _assignmentService;

    public ExerciseService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper,
        PullRequestService pullRequestService, AssignmentService assignmentService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _pullRequestService = pullRequestService;
        _assignmentService = assignmentService;
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

    public async Task<Data.Models.Exercise> GetExerciseModelByGitHubRepoNameAsync(string githubRepoName)
    {
        return await _gradeManagementDbContext.Exercise
            .Include(e => e.Course)
            .Include(e => e.Assignments)
            .SingleEntityAsync(e => githubRepoName.StartsWith(e.GithubPrefix), 0);
    }

    public async Task<string> GetCsvByExerciseId(long exerciseId)
    {
        var assignments = await _gradeManagementDbContext.Assignment
            .Where(a => a.ExerciseId == exerciseId)
            .Include(assignment => assignment.Student)
            .ToListAsync();

        var records = new List<Dictionary<string, object>>();

        foreach (var assignment in assignments)
        {
            var pullRequest = await _assignmentService.GetMergedPullRequestModelByIdAsync(assignment.Id);
            if (pullRequest == null)
            {
                continue;
            }

            var scores = await _pullRequestService.GetApprovedScoreModelsByIdAsync(pullRequest.Id);
            var record = new Dictionary<string, object>
            {
                { "NeptunCode", assignment.Student.NeptunCode }, { "SumOfScores", scores.Sum(s => s.Value) }
            };
            foreach (var score in scores)
            {
                record[score.ScoreType.Type] = score.Value;
            }


            records.Add(record);
        }

        await using var writer = new StringWriter();
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        // Write the header
        var keys = records[0].Keys;
        foreach (var key in keys)
        {
            csv.WriteField(key);
        }

        await csv.NextRecordAsync();

        // Write the records
        foreach (var record in records)
        {
            foreach (var value in record.Values)
            {
                csv.WriteField(value);
            }

            await csv.NextRecordAsync();
        }

        await csv.FlushAsync();

        return writer.ToString();
    }
}
