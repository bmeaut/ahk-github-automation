using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.EntityFrameworkCore;

using Task = System.Threading.Tasks.Task;

namespace GradeManagement.Bll;

public class SubjectService : ICrudServiceBase<Subject, Shared.Dtos.Response.Subject>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly SubjectTeacherService _subjectTeacherService;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper,
        SubjectTeacherService subjectTeacherService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _subjectTeacherService = subjectTeacherService;
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

        var teachers = await _gradeManagementDbContext.User
            .Where(t => requestDto.Teachers.Select(rqT => rqT.Id).Contains(t.Id))
            .ToListAsync();
        var subjectTeachers = await _subjectTeacherService.UpdateForSingleSubjectAsync(subjectEntity.Id, teachers);

        subjectEntity.Name = requestDto.Name;
        subjectEntity.NeptunCode = requestDto.NeptunCode;
        //subjectEntity.SubjectTeachers = subjectTeachers; TODO check if needed

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Shared.Dtos.Response.Subject>(subjectEntity);
    }

    public async Task<Shared.Dtos.Response.Subject> CreateAsync(Subject requestDto)
    {
        var teachers = await _gradeManagementDbContext.User
            .Where(t => requestDto.Teachers.Select(rqT => rqT.Id).Contains(t.Id))
            .ToListAsync();
        var subjectTeachers = await _subjectTeacherService.UpdateForSingleSubjectAsync(requestDto.Id, teachers);
        var subjectEntity = new Data.Models.Subject
        {
            Name = requestDto.Name, NeptunCode = requestDto.NeptunCode, Courses = [],
            //SubjectTeachers = subjectTeachers TODO check if needed
        };
        _gradeManagementDbContext.Subject.Add(subjectEntity);
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
        var selectedSubjectEntity = await _gradeManagementDbContext.Subject
            .Include(s => s.Courses)
            .ThenInclude(c=>c.Semester)
            .Include(s => s.Courses)
            .ThenInclude(c=>c.Language)
            .Select(s => new { Id = s.Id, Courses = s.Courses })
            .SingleEntityAsync(s => s.Id == id, id);
        return _mapper.Map<List<Course>>(selectedSubjectEntity.Courses);
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
