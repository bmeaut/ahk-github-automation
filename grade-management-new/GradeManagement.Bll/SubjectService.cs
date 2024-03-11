using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

using Subject = GradeManagement.Shared.Dtos.Subject;
using Task = System.Threading.Tasks.Task;

namespace GradeManagement.Bll;

public class SubjectService : ICrudServiceBase<Subject>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public SubjectService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Subject>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Subject
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Subject> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Subject
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Subject> UpdateAsync(long id, Subject requestDto)
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

        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Subject>(subjectEntity);
    }

    public async Task<Subject> CreateAsync(Subject requestDto)
    {
        var subjectEntity = new Data.Models.Subject
        {
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            Courses = []
        };
        _gradeManagementDbContext.Subject.Add(subjectEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Subject>(subjectEntity);
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
}
