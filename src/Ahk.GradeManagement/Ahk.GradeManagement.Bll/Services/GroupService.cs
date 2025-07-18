using Ahk.GradeManagement.Backend.Common.RequestContext;
using Ahk.GradeManagement.Bll.Services.BaseServices;
using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Shared.Dtos.Request;
using Ahk.GradeManagement.Shared.Dtos.Response;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using Microsoft.EntityFrameworkCore;

using GroupTeacher = Ahk.GradeManagement.Dal.Entities.GroupTeacher;
using User = Ahk.GradeManagement.Shared.Dtos.User;

namespace Ahk.GradeManagement.Bll.Services;

public class GroupService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper,
    UserService userService,
    CourseService courseService,
    IRequestContext requestContext)
    : ICrudServiceBase<GroupRequest, GroupResponse>
{
    public async Task<IEnumerable<GroupResponse>> GetAllAsync()
    {
        return await gradeManagementDbContext.Group
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .OrderBy(g => g.Id).ToListAsync();
    }

    public async Task<GroupResponse> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Group
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .SingleEntityAsync(g => g.Id == id, id);
    }

    public async Task<GroupResponse> CreateAsync(GroupRequest requestDto)
    {
        await courseService.GetByIdAsync(requestDto.CourseId);
        var groupEntity = new Group
        {
            Name = requestDto.Name,
            CourseId = requestDto.CourseId,
            SubjectId = requestContext.CurrentUser.CurrentSubjectId.Value
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
        return mapper.Map<GroupResponse>(groupEntity);
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

    public async Task<GroupResponse> UpdateAsync(long id, GroupRequest requestDto)
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
        return mapper.Map<GroupResponse>(groupEntity);
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        return await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.GroupId == id)
            .Select(gt => gt.User)
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<StudentResponse>> GetAllStudentsByIdAsync(long id)
    {
        return await gradeManagementDbContext.GroupStudent
            .Where(gs => gs.GroupId == id)
            .Select(gs => gs.Student)
            .ProjectTo<StudentResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
