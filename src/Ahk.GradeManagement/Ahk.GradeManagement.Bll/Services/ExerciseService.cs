using Ahk.GradeManagement.Bll.Services.BaseServices;
using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Dal.Extensions;
using Ahk.GradeManagement.Shared.Dtos;
using Ahk.GradeManagement.Shared.Dtos.Request;
using Ahk.GradeManagement.Shared.Dtos.Response;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using CsvHelper;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using Assignment = Ahk.GradeManagement.Shared.Dtos.Assignment;
using ValidationException = AutSoft.Common.Exceptions.ValidationException;

namespace Ahk.GradeManagement.Bll.Services;

public class ExerciseService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper,
    PullRequestService pullRequestService,
    AssignmentService assignmentService,
    ScoreTypeService scoreTypeService)
    : ICrudServiceBase<ExerciseRequest, ExerciseResponse>
{
    public async Task<IEnumerable<ExerciseResponse>> GetAllAsync()
    {
        var exercises = await gradeManagementDbContext.Exercise
            .Include(e => e.ScoreTypeExercises).ThenInclude(ste => ste.ScoreType)
            .ProjectTo<ExerciseResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
        return exercises;
    }

    public async Task<ExerciseResponse> GetByIdAsync(long id)
    {
        var exerciseEntity = await gradeManagementDbContext.Exercise
            .Include(e => e.ScoreTypeExercises)
            .ThenInclude(ste => ste.ScoreType)
            .SingleEntityAsync(e => e.Id == id, id);

        return mapper.Map<ExerciseResponse>(exerciseEntity);
    }

    public async Task<ExerciseResponse> CreateAsync(ExerciseRequest requestDto)
    {
        Exercise exerciseEntity;
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            exerciseEntity = new Exercise()
            {
                Name = requestDto.Name,
                GithubPrefix = requestDto.GithubPrefix,
                ClassroomUrl = requestDto.ClassroomUrl,
                MoodleScoreNamePrefix = requestDto.MoodleScoreNamePrefix,
                DueDate = requestDto.DueDate,
                CourseId = requestDto.CourseId,
                SubjectId = gradeManagementDbContext.SubjectIdValue
            };
            gradeManagementDbContext.Exercise.Add(exerciseEntity);
            await gradeManagementDbContext.SaveChangesAsync();
            exerciseEntity = await gradeManagementDbContext.Exercise
                .SingleEntityAsync(e => e.Id == exerciseEntity.Id, exerciseEntity.Id);
            exerciseEntity.ScoreTypeExercises =
                await GetScoreTypeExercisesByTypeAndOrdernAsync(requestDto.ScoreTypes, exerciseEntity.Id);
            await gradeManagementDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await GetByIdAsync(exerciseEntity.Id);
    }

    public async Task<ExerciseResponse> UpdateAsync(long id, ExerciseRequest requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var exerciseEntity = await gradeManagementDbContext.Exercise
            .SingleEntityAsync(e => e.Id == id, id);

        exerciseEntity.Name = requestDto.Name;
        exerciseEntity.GithubPrefix = requestDto.GithubPrefix;
        exerciseEntity.DueDate = requestDto.DueDate;
        exerciseEntity.CourseId = requestDto.CourseId;
        exerciseEntity.ScoreTypeExercises =
            await GetScoreTypeExercisesByTypeAndOrderInTransactionAsync(requestDto.ScoreTypes, id);

        await gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(exerciseEntity.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var exerciseEntity = await gradeManagementDbContext.Exercise.SingleEntityAsync(e => e.Id == id, id);
        gradeManagementDbContext.Exercise.Remove(exerciseEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }


    public async Task<IEnumerable<Assignment>> GetAssignmentsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Assignment
            .Where(a => a.ExerciseId == id)
            .ProjectTo<Assignment>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Exercise> GetExerciseModelByGitHubRepoNameWithoutQfAsync(string githubRepoName)
    {
        return await gradeManagementDbContext.Exercise
            .IgnoreQueryFiltersButNotIsDeleted()
            .Include(e => e.Course)
            .Include(e => e.Assignments)
            .SingleEntityAsync(e => githubRepoName.StartsWith(e.GithubPrefix), 0);
    }

    private async Task<List<Dal.Entities.ScoreTypeExercise>> GetScoreTypeExercisesByTypeAndOrderInTransactionAsync(
        Dictionary<int, string> scoreTypes, long exerciseId)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            await GetScoreTypeExercisesByTypeAndOrdernAsync(scoreTypes, exerciseId);

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return await gradeManagementDbContext.ScoreTypeExercise.Where(s => s.ExerciseId == exerciseId).ToListAsync();
    }

    private async Task<List<Dal.Entities.ScoreTypeExercise>> GetScoreTypeExercisesByTypeAndOrdernAsync(
        Dictionary<int, string> scoreTypes, long exerciseId)
    {
        foreach (var (order, type) in scoreTypes)
        {
            var scoreType = await scoreTypeService.GetOrCreateScoreTypeByTypeStringAsync(type);
            gradeManagementDbContext.ScoreTypeExercise.Add(new Dal.Entities.ScoreTypeExercise
            {
                ScoreTypeId = scoreType.Id, ExerciseId = exerciseId, Order = order
            });
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return await gradeManagementDbContext.ScoreTypeExercise.Where(s => s.ExerciseId == exerciseId).ToListAsync();
    }

    public async Task<Dal.Entities.ScoreType> GetScoreTypeByOrderAndExerciseIdAsync(int order, long exerciseId)
    {
        var scoreTypeExercise = await gradeManagementDbContext.ScoreTypeExercise
            .Include(ste => ste.ScoreType)
            .SingleEntityAsync(ste => ste.ExerciseId == exerciseId && ste.Order == order, 0);
        return scoreTypeExercise.ScoreType;
    }

    public async Task<IEnumerable<Shared.Dtos.ScoreTypeExercise>> GetScoreTypeExercisesByIdAsync(long id)
    {
        return await gradeManagementDbContext.ScoreTypeExercise
            .Where(ste => ste.ExerciseId == id)
            .OrderBy(ste => ste.Order)
            .Include(ste => ste.ScoreType)
            .ProjectTo<Shared.Dtos.ScoreTypeExercise>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task SetMoodleScoreUrlByClassroomUrlWithoutQueryFilterAsync(string clasroomUrl, string moodleScoreUrl)
    {
        var exercise = await gradeManagementDbContext.Exercise.IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(e => e.ClassroomUrl == clasroomUrl, 0);
        if (exercise.MoodleScoreUrl == moodleScoreUrl) return;
        exercise.MoodleScoreUrl = moodleScoreUrl;
        await gradeManagementDbContext.SaveChangesAsync();
    }
}
