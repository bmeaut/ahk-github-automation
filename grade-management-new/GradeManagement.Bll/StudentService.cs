using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class StudentService : IQueryServiceBase<Student>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public StudentService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Student>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Student
            .ProjectTo<Student>(_mapper.ConfigurationProvider)
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public async Task<Student> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Student
            .ProjectTo<Student>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Student> CreateAsync(Student requestDto)
    {
        var studentEntity = new Data.Models.Student()
        {
            Id = requestDto.Id,
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            GithubId = requestDto.GithubId
        };
        _gradeManagementDbContext.Student.Add(studentEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return await GetByIdAsync(studentEntity.Id);
    }

    public async Task<List<Group>> GetAllGroupsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Student
            .Where(s => s.Id == id)
            .SelectMany(s => s.GroupStudents)
            .Select(gt => gt.Group)
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Assignment>> GetAllAssignmentsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Assignment
            .Where(a => a.StudentId == id)
            .ProjectTo<Assignment>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
