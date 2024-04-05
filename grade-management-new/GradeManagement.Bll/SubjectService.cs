using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;
using GradeManagement.Data.Data;
using Microsoft.EntityFrameworkCore;
using Course = GradeManagement.Data.Models.Course;
using Subject = GradeManagement.Shared.Dtos.Subject;
using Task = System.Threading.Tasks.Task;
using User = GradeManagement.Shared.Dtos.User;

namespace GradeManagement.Bll;

public class SubjectService : ICrudServiceBase<Subject, Shared.Dtos.Response.Subject>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly UserService _userService;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper,
        UserService userService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<IEnumerable<Shared.Dtos.Response.Subject>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Subject
            .ProjectTo<Shared.Dtos.Response.Subject>(_mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Shared.Dtos.Response.Subject> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Subject
            .ProjectTo<Shared.Dtos.Response.Subject>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Shared.Dtos.Response.Subject> UpdateAsync(long id, Subject requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var subjectEntity = await _gradeManagementDbContext.Subject
            .SingleEntityAsync(s => s.Id == id, id);

        subjectEntity.Name = requestDto.Name;
        subjectEntity.NeptunCode = requestDto.NeptunCode;

        var teachers = await _userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        var oldSubjectTeachers = await _gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == subjectEntity.Id)
            .ToListAsync();
        _gradeManagementDbContext.SubjectTeacher.RemoveRange(oldSubjectTeachers);
        foreach (var teacher in teachers)
        {
            _gradeManagementDbContext.SubjectTeacher.Add(new SubjectTeacher
            {
                SubjectId = subjectEntity.Id, UserId = teacher.Id
            });
        }

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Shared.Dtos.Response.Subject>(subjectEntity);
    }

    public async Task<Shared.Dtos.Response.Subject> CreateAsync(Subject requestDto)
    {
        var subjectEntity = new Data.Models.Subject
        {
            Name = requestDto.Name, NeptunCode = requestDto.NeptunCode, Courses = [],
        };
        _gradeManagementDbContext.Subject.Add(subjectEntity);
        await _gradeManagementDbContext.SaveChangesAsync();

        var teachers = await _userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        foreach (var teacher in teachers)
        {
            _gradeManagementDbContext.SubjectTeacher.Add(new SubjectTeacher
            {
                SubjectId = subjectEntity.Id, UserId = teacher.Id
            });
        }

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Shared.Dtos.Response.Subject>(subjectEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var subject = await _gradeManagementDbContext.Subject.SingleEntityAsync(s => s.Id == id, id);
        _gradeManagementDbContext.Subject.Remove(subject);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<List<Course>> GetAllCoursesByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .Where(c => c.SubjectId == id)
            .ProjectTo<Course>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        var selctedSubjectEntity = await _gradeManagementDbContext.Subject
            .Include(s => s.SubjectTeachers).ThenInclude(st => st.User)
            .Select(s => new { Id = s.Id, SubjectTeachers = s.SubjectTeachers })
            .SingleEntityAsync(s => s.Id == id, id);
        return _mapper.Map<List<User>>(selctedSubjectEntity.SubjectTeachers.Select(st => st.User));
    }
}
