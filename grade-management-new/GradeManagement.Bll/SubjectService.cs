using AutoMapper;
using AutoMapper.QueryableExtensions;
using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;
using GradeManagement.Data.Data;
using Microsoft.EntityFrameworkCore;
using Course = GradeManagement.Data.Models.Course;
using Subject = GradeManagement.Shared.Dtos.Subject;
using Task = System.Threading.Tasks.Task;

namespace GradeManagement.Bll;

public class SubjectService
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Subject>> GetAllSubjectsAsync()
    {
        return await _gradeManagementDbContext.Subject
            .Include(s => s.Courses)
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Subject> GetSubjectByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Subject
            .Include(s => s.Courses)
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Subject> UpdateSubjectAsync(long id, Subject subjectDto)
    {
        if (subjectDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        Data.Models.Subject subjectEntity = await _gradeManagementDbContext.Subject.Include(s => s.Courses)
            .SingleEntityAsync(s => s.Id == id, id);

        subjectEntity.Name = subjectDto.Name;
        subjectEntity.NeptunCode = subjectDto.NeptunCode;

        var courses = _gradeManagementDbContext.Course
            .Where(c => subjectDto.Courses.Select(co => co.Id).Contains(c.Id)).ToList();
        List<Course> coursesToDelete = new List<Course>();
        foreach (var course in subjectEntity.Courses)
        {
            if (courses.All(c => c.Id != course.Id))
            {
                coursesToDelete.Add(course);
            }
        }

        foreach (var course in coursesToDelete)
        {
            subjectEntity.Courses.RemoveAll(c => c.Id == course.Id);
        }

        foreach (var course in courses)
        {
            if (subjectEntity.Courses.All(c => c.Id != course.Id))
            {
                subjectEntity.Courses.Add(course);
            }
        }

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Subject>(subjectEntity);
    }

    public async Task<Subject> CreateSubjectAsync(Subject subject)
    {
        Data.Models.Subject subjectEntity = new Data.Models.Subject
        {
            Name = subject.Name,
            NeptunCode = subject.NeptunCode,
            Courses = _gradeManagementDbContext.Course
                .Where(c => subject.Courses.Select(co => co.Id).Contains(c.Id)).ToList()
        };
        _gradeManagementDbContext.Subject.Add(subjectEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Subject>(subjectEntity);
    }

    public async Task DeleteSubjectAsync(long id)
    {
        var subject = await _gradeManagementDbContext.Subject.SingleEntityAsync(s => s.Id == id, id);
        _gradeManagementDbContext.Subject.Remove(subject);
        await _gradeManagementDbContext.SaveChangesAsync();
    }
}
