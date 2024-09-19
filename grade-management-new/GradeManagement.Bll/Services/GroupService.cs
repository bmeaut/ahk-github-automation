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

public class GroupService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper,
    UserService userService,
    CourseService courseService)
    : ICrudServiceBase<Group, Shared.Dtos.Response.Group>
{
    public async Task<IEnumerable<Shared.Dtos.Response.Group>> GetAllAsync()
    {
        return await gradeManagementDbContext.Group
            .ProjectTo<Shared.Dtos.Response.Group>(mapper.ConfigurationProvider)
            .OrderBy(g => g.Id).ToListAsync();
    }

    public async Task<Shared.Dtos.Response.Group> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Group
            .ProjectTo<Shared.Dtos.Response.Group>(mapper.ConfigurationProvider)
            .SingleEntityAsync(g => g.Id == id, id);
    }

    public async Task<Shared.Dtos.Response.Group> CreateAsync(Group requestDto)
    {
        await courseService.GetByIdAsync(requestDto.CourseId);
        var groupEntity = new Data.Models.Group
        {
            Name = requestDto.Name,
            CourseId = requestDto.CourseId,
            SubjectId = gradeManagementDbContext.SubjectIdValue
        };
        gradeManagementDbContext.Group.Add(groupEntity);
        await gradeManagementDbContext.SaveChangesAsync();
        var teachers = await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        foreach (var teacher in teachers)
        {
            gradeManagementDbContext.GroupTeacher.Add(
                new GroupTeacher { GroupId = groupEntity.Id, UserId = teacher.Id });
        }

        await gradeManagementDbContext.SaveChangesAsync();
        return mapper.Map<Shared.Dtos.Response.Group>(groupEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var groupEntity = await gradeManagementDbContext.Group
            .SingleEntityAsync(g => g.Id == id, id);
        var groupTeachers = await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .ToListAsync();
        var groupStudents = await gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .ToListAsync();
        gradeManagementDbContext.GroupTeacher.RemoveRange(groupTeachers);
        gradeManagementDbContext.GroupStudent.RemoveRange(groupStudents);
        gradeManagementDbContext.Group.Remove(groupEntity);

        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<Shared.Dtos.Response.Group> UpdateAsync(long id, Group requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var groupEntity = await gradeManagementDbContext.Group
            .SingleEntityAsync(g => g.Id == id, id);
        groupEntity.Name = requestDto.Name;
        groupEntity.CourseId = requestDto.CourseId;

        var teachers = await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        var oldGroupTeachers = await gradeManagementDbContext.GroupTeacher.Where(gt => gt.GroupId == id).ToListAsync();
        gradeManagementDbContext.GroupTeacher.RemoveRange(oldGroupTeachers);
        foreach (var teacher in teachers)
        {
            gradeManagementDbContext.GroupTeacher.Add(new GroupTeacher { GroupId = id, UserId = teacher.Id });
        }

        await gradeManagementDbContext.SaveChangesAsync();
        return mapper.Map<Shared.Dtos.Response.Group>(groupEntity);
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        return await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .Select(gt => gt.User)
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Student>> GetAllStudentsByIdAsync(long id)
    {
        return await gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .Select(gs => gs.Student)
            .ProjectTo<Student>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
