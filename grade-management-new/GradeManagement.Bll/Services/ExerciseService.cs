﻿using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using CsvHelper;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Data.Models;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using Assignment = GradeManagement.Shared.Dtos.Assignment;
using Exercise = GradeManagement.Shared.Dtos.Request.Exercise;
using ValidationException = AutSoft.Common.Exceptions.ValidationException;

namespace GradeManagement.Bll.Services;

public class ExerciseService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper,
    PullRequestService pullRequestService,
    AssignmentService assignmentService,
    ScoreTypeService scoreTypeService,
    CourseService courseService)
    : ICrudServiceBase<Exercise, Shared.Dtos.Response.Exercise>
{
    public async Task<IEnumerable<Shared.Dtos.Response.Exercise>> GetAllAsync()
    {
        return await gradeManagementDbContext.Exercise
            .Include(e => e.ScoreTypeExercises)
            .ProjectTo<Shared.Dtos.Response.Exercise>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Shared.Dtos.Response.Exercise> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Exercise
            .Include(e => e.ScoreTypeExercises)
            .ProjectTo<Shared.Dtos.Response.Exercise>(mapper.ConfigurationProvider)
            .SingleEntityAsync(e => e.Id == id, id);
    }

    public async Task<Shared.Dtos.Response.Exercise> CreateAsync(Exercise requestDto)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            await courseService.GetByIdAsync(requestDto.CourseId); // Check if the course exists and user has access
            var exerciseEntity = new Data.Models.Exercise()
            {
                Name = requestDto.Name,
                GithubPrefix = requestDto.GithubPrefix,
                dueDate = requestDto.dueDate,
                CourseId = requestDto.CourseId,
                SubjectId = gradeManagementDbContext.SubjectIdValue
            };
            gradeManagementDbContext.Exercise.Add(exerciseEntity);
            await gradeManagementDbContext.SaveChangesAsync();
            exerciseEntity = await gradeManagementDbContext.Exercise
                .SingleEntityAsync(e => e.Id == exerciseEntity.Id, exerciseEntity.Id);
            exerciseEntity.ScoreTypeExercises =
                await GetScoreTypeExercisesByTypeAndOrderAsync(requestDto.ScoreTypes, exerciseEntity.Id);
            await gradeManagementDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
            return await GetByIdAsync(exerciseEntity.Id);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Shared.Dtos.Response.Exercise> UpdateAsync(long id, Exercise requestDto)
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
        exerciseEntity.dueDate = requestDto.dueDate;
        exerciseEntity.CourseId = requestDto.CourseId;
        exerciseEntity.ScoreTypeExercises = await GetScoreTypeExercisesByTypeAndOrderAsync(requestDto.ScoreTypes, id);

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

    public async Task<Data.Models.Exercise> GetExerciseModelByGitHubRepoNameAsync(string githubRepoName)
    {
        return await gradeManagementDbContext.Exercise
            .Include(e => e.Course)
            .Include(e => e.Assignments)
            .SingleEntityAsync(e => githubRepoName.StartsWith(e.GithubPrefix), 0);
    }

    public async Task<List<ScoreTypeExercise>> GetScoreTypeExercisesByTypeAndOrderAsync(
        Dictionary<int, string> scoreTypes, long _ExerciseId)
    {
        await using var transaction = await gradeManagementDbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (var (order, type) in scoreTypes)
            {
                var scoreType = await scoreTypeService.GetOrCreateScoreTypeByTypeStringAsync(type);
                gradeManagementDbContext.ScoreTypeExercise.Add(new ScoreTypeExercise
                {
                    ScoreTypeId = scoreType.Id, ExerciseId = _ExerciseId, Order = order
                });
            }

            await gradeManagementDbContext.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return gradeManagementDbContext.ScoreTypeExercise.Where(s => s.ExerciseId == _ExerciseId).ToList();
    }

    public async Task<string> GetCsvByExerciseId(long exerciseId)
    {
        var assignments = await gradeManagementDbContext.Assignment
            .Where(a => a.ExerciseId == exerciseId)
            .Include(assignment => assignment.Student)
            .ToListAsync();

        var records = new List<Dictionary<string, object>>();

        foreach (var assignment in assignments)
        {
            var pullRequest = await assignmentService.GetMergedPullRequestModelByIdAsync(assignment.Id);
            if (pullRequest == null)
            {
                continue;
            }

            var scores = await pullRequestService.GetApprovedScoreModelsByIdAsync(pullRequest.Id);
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

    public async Task<IEnumerable<Shared.Dtos.ScoreTypeExercise>> GetScoreTypeExercisesByIdAsync(long id)
    {
        return await gradeManagementDbContext.ScoreTypeExercise
            .Where(ste => ste.ExerciseId == id)
            .OrderBy(ste => ste.Order)
            .Include(ste => ste.ScoreType)
            .ProjectTo<Shared.Dtos.ScoreTypeExercise>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
