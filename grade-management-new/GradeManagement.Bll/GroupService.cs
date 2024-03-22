using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class GroupService : ICrudServiceBase<Group>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;
    private readonly GroupTeacherService _groupTeacherService;

    public GroupService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper,
        GroupTeacherService groupTeacherService)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
        _groupTeacherService = groupTeacherService;
    }

    public async Task<IEnumerable<Group>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Group
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .OrderBy(g => g.Id).ToListAsync();
    }

    public async Task<Group> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Group
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(g => g.Id == id, id);
    }

    public async Task<Group> CreateAsync(Group requestDto)
    {
        var groupEntity = new Data.Models.Group { Name = requestDto.Name, CourseId = requestDto.CourseId };
        var teachers = await _gradeManagementDbContext.User
            .Where(t => requestDto.Teachers.Select(rqT => rqT.Id).Contains(t.Id))
            .ToListAsync();
        var groupTeachers = await _groupTeacherService.UpdateTeachersByGroupIdAsync(groupEntity.Id, teachers);
        groupEntity.GroupTeachers = groupTeachers;
        _gradeManagementDbContext.Group.Add(groupEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Group>(groupEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var groupEntity = await _gradeManagementDbContext.Group
            .SingleEntityAsync(g => g.Id == id, id);
        _gradeManagementDbContext.Group.Remove(groupEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<Group> UpdateAsync(long id, Group requestDto)
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
        var teachers = await _gradeManagementDbContext.User
            .Where(t => requestDto.Teachers.Select(rqT => rqT.Id).Contains(t.Id))
            .ToListAsync();
        var groupTeachers = await _groupTeacherService.UpdateTeachersByGroupIdAsync(groupEntity.Id, teachers);
        groupEntity.GroupTeachers = groupTeachers;
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<Group>(groupEntity);
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        return await _gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .Select(gt => gt.User)
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllStudentsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .Select(gs => gs.Student)
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
