using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Request;

using Microsoft.EntityFrameworkCore;

using GroupTeacher = GradeManagement.Data.Models.GroupTeacher;
using Student = GradeManagement.Shared.Dtos.Response.Student;

namespace GradeManagement.Bll.Services;

public class GroupService : ICrudServiceBase<Group, Shared.Dtos.Response.Group>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly UserService _userService;

    public GroupService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper,
        UserService userService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _userService = userService;
    }

    public async Task<IEnumerable<Shared.Dtos.Response.Group>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Group
            .ProjectTo<Shared.Dtos.Response.Group>(_mapper.ConfigurationProvider)
            .OrderBy(g => g.Id).ToListAsync();
    }

    public async Task<Shared.Dtos.Response.Group> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Group
            .ProjectTo<Shared.Dtos.Response.Group>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(g => g.Id == id, id);
    }

    public async Task<Shared.Dtos.Response.Group> CreateAsync(Group requestDto)
    {
        var groupEntity = new Data.Models.Group { Name = requestDto.Name, CourseId = requestDto.CourseId };
        _gradeManagementDbContext.Group.Add(groupEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        var teachers = await _userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        foreach (var teacher in teachers)
        {
            _gradeManagementDbContext.GroupTeacher.Add(new GroupTeacher
            {
                GroupId = groupEntity.Id, UserId = teacher.Id
            });
        }

        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Shared.Dtos.Response.Group>(groupEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var groupEntity = await _gradeManagementDbContext.Group
            .SingleEntityAsync(g => g.Id == id, id);
        var groupTeachers = await _gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .ToListAsync();
        var groupStudents = await _gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .ToListAsync();
        _gradeManagementDbContext.GroupTeacher.RemoveRange(groupTeachers);
        _gradeManagementDbContext.GroupStudent.RemoveRange(groupStudents);
        _gradeManagementDbContext.Group.Remove(groupEntity);

        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<Shared.Dtos.Response.Group> UpdateAsync(long id, Group requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var groupEntity = await _gradeManagementDbContext.Group
            .SingleEntityAsync(g => g.Id == id, id);
        groupEntity.Name = requestDto.Name;
        groupEntity.CourseId = requestDto.CourseId;

        var teachers = await _userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        var oldGroupTeachers = await _gradeManagementDbContext.GroupTeacher.Where(gt => gt.GroupId == id).ToListAsync();
        _gradeManagementDbContext.GroupTeacher.RemoveRange(oldGroupTeachers);
        foreach (var teacher in teachers)
        {
            _gradeManagementDbContext.GroupTeacher.Add(new GroupTeacher { GroupId = id, UserId = teacher.Id });
        }

        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Shared.Dtos.Response.Group>(groupEntity);
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        return await _gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .Select(gt => gt.User)
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Student>> GetAllStudentsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .Select(gs => gs.Student)
            .ProjectTo<Student>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
