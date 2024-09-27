using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Server.Authorization;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;
using GradeManagement.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace GradeManagement.Bll.Services;

public class CourseService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    : ICrudServiceBase<Course>
{
    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<Course>(mapper.ConfigurationProvider)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<Course> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<Course>(mapper.ConfigurationProvider)
            .SingleEntityAsync(c => c.Id == id, id);
    }

    public async Task<Course> UpdateAsync(long id, Course requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var courseEntity = await gradeManagementDbContext.Course
            .SingleEntityAsync(c => c.Id == id, id);
        courseEntity.Name = requestDto.Name;
        courseEntity.MoodleCourseId = requestDto.MoodleCourseId;
        courseEntity.SubjectId = requestDto.SubjectId;
        courseEntity.SemesterId = requestDto.Semester.Id;
        courseEntity.LanguageId = requestDto.Language.Id;

        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(courseEntity.Id);
    }

    public async Task<Course> CreateAsync(Course requestDto)
    {
        if (requestDto.SubjectId != gradeManagementDbContext.SubjectIdValue)
        {
            throw new UnauthorizedException();
        }
        var courseEntityToBeCreated = new Data.Models.Course
        {
            Id = requestDto.Id,
            Name = requestDto.Name,
            MoodleCourseId = requestDto.MoodleCourseId,
            SubjectId = requestDto.SubjectId,
            SemesterId = requestDto.Semester.Id,
            LanguageId = requestDto.Language.Id
        };
        gradeManagementDbContext.Course.Add(courseEntityToBeCreated);
        await gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(courseEntityToBeCreated.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var courseEntity = await gradeManagementDbContext.Course.SingleEntityAsync(c => c.Id == id, id);
        gradeManagementDbContext.Course.Remove(courseEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ExerciseResponse>> GetAllExercisesByIdAsync(long id)
    {
        return await gradeManagementDbContext.Exercise
            .Where(e => e.CourseId == id)
            .ProjectTo<ExerciseResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<IEnumerable<GroupResponse>> GetAllGroupsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Group
            .Where(g => g.CourseId == id)
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
