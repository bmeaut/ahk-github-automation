using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class CourseService : ICrudServiceBase<Course>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public CourseService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<Course>(_mapper.ConfigurationProvider)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<Course> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<Course>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(c => c.Id == id, id);
    }

    public async Task<Course> UpdateAsync(long id, Course requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var courseEntity = await _gradeManagementDbContext.Course
            .SingleEntityAsync(c => c.Id == id, id);
        courseEntity.Name = requestDto.Name;
        courseEntity.MoodleCourseId = requestDto.MoodleCourseId;
        courseEntity.SubjectId = requestDto.SubjectId;
        courseEntity.SemesterId = requestDto.Semester.Id;
        courseEntity.LanguageId = requestDto.Language.Id;

        await _gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(courseEntity.Id);
    }

    public async Task<Course> CreateAsync(Course requestDto)
    {
        var courseEntityToBeCreated = new Data.Models.Course
        {
            Id = requestDto.Id,
            Name = requestDto.Name,
            MoodleCourseId = requestDto.MoodleCourseId,
            SubjectId = requestDto.SubjectId,
            SemesterId = requestDto.Semester.Id,
            LanguageId = requestDto.Language.Id
        };
        _gradeManagementDbContext.Course.Add(courseEntityToBeCreated);
        await _gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(courseEntityToBeCreated.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var courseEntity = await _gradeManagementDbContext.Course.SingleEntityAsync(c => c.Id == id, id);
        _gradeManagementDbContext.Course.Remove(courseEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<Exercise>> GetAllExercisesByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Exercise
            .Where(e => e.CourseId == id)
            .ProjectTo<Exercise>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetAllGroupsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Group
            .Where(g => g.CourseId == id)
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
