using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

using Task = System.Threading.Tasks.Task;

namespace GradeManagement.Bll;

public class SubjectService : ICrudServiceBase<Shared.Dtos.Request.Subject,Shared.Dtos.Response.Subject>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly SubjectTeacherService _subjectTeacherService;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
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

    public async Task<Shared.Dtos.Response.Subject> UpdateAsync(long id, Shared.Dtos.Request.Subject requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var subjectEntity = await _gradeManagementDbContext.Subject
            .SingleEntityAsync(s => s.Id == id, id);

        var teachers = await _gradeManagementDbContext.Teacher
            .Where(t => requestDto.Teachers.Select(rqT=>rqT.Id).Contains(t.Id))
            .ToListAsync();
        var subjectTeachers = await _subjectTeacherService.UpdateForSingleSubjectAsync(subjectEntity.Id, teachers);

        subjectEntity.Name = requestDto.Name;
        subjectEntity.NeptunCode = requestDto.NeptunCode;
        //subjectEntity.SubjectTeachers = subjectTeachers; TODO check if needed

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Shared.Dtos.Response.Subject>(subjectEntity);
    }

    public async Task<GradeManagement.Shared.Dtos.Response.Subject> CreateAsync(Shared.Dtos.Request.Subject requestDto)
    {
        var teachers = await _gradeManagementDbContext.Teacher
            .Where(t => requestDto.Teachers.Select(rqT=>rqT.Id).Contains(t.Id))
            .ToListAsync();
        var subjectTeachers = await _subjectTeacherService.UpdateForSingleSubjectAsync(requestDto.Id, teachers);
        var subjectEntity = new Data.Models.Subject
        {
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            Courses = [],
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
            .Include(s => s.Courses).Select(s => new { Id = s.Id, Courses = s.Courses })
            .SingleEntityAsync(s => s.Id == id, id);
        return _mapper.Map<List<Course>>(selectedSubjectEntity.Courses);
    }

    public async Task<List<Teacher>> GetAllTeachersByIdAsync(long id)
    {
        var selctedSubjectEntity = await _gradeManagementDbContext.Subject
            .Include(s => s.SubjectTeachers).ThenInclude(st => st.Teacher)
            .Select(s => new { Id = s.Id, SubjectTeachers = s.SubjectTeachers })
            .SingleEntityAsync(s => s.Id == id, id);
        return _mapper.Map<List<Teacher>>(selctedSubjectEntity.SubjectTeachers.Select(st => st.Teacher));
    }
}
