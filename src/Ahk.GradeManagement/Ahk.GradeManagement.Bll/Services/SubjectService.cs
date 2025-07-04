using Ahk.GradeManagement.Bll.Services.BaseServices;
using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Dal.Extensions;
using Ahk.GradeManagement.Shared.Dtos.Request;
using Ahk.GradeManagement.Shared.Dtos.Response;
using Ahk.GradeManagement.Shared.Enums;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

using Task = System.Threading.Tasks.Task;
using User = Ahk.GradeManagement.Shared.Dtos.User;

namespace Ahk.GradeManagement.Bll.Services;

public class SubjectService(
    GradeManagementDbContext gradeManagementDbContext,
    IMapper mapper,
    UserService userService)
    : ICrudServiceBase<SubjectRequest, SubjectResponse>
{
    public async Task<IEnumerable<SubjectResponse>> GetAllAsync()
    {
        return await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .ProjectTo<SubjectResponse>(mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<IEnumerable<SubjectResponse>> GetAllAsyncForUserWithoutQf(ClaimsPrincipal httpContextUser)
    {
        var roleClaim = AuthorizationHelper.GetCurrentUserRole(httpContextUser);
        if (roleClaim != null && roleClaim == UserType.Admin.ToString())
            return await GetAllAsync();

        var user = await userService.GetCurrentUserAsync(httpContextUser);
        return await gradeManagementDbContext.SubjectTeacher
            .IgnoreQueryFiltersButNotIsDeleted()
            .Where(su => su.UserId == user.Id)
            .Select(su => su.Subject)
            .ProjectTo<SubjectResponse>(mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<SubjectResponse> GetByIdForUserWithoutQfAsync(long id, ClaimsPrincipal httpContextUser)
    {
        var roleClaim = AuthorizationHelper.GetCurrentUserRole(httpContextUser);
        if (roleClaim != null && roleClaim == UserType.Admin.ToString())
            return await GetByIdAsync(id);

        var subjectsForUser = await GetAllAsyncForUserWithoutQf(httpContextUser);

        var subject = await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .ProjectTo<SubjectResponse>(mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);

        if (subjectsForUser.Any(s => s.Id == id))
            return subject;

        throw new ValidationException("Id", id.ToString(), "The user does not have access to this subject!");
    }

    public async Task<SubjectResponse> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .ProjectTo<SubjectResponse>(mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<SubjectResponse> UpdateAsync(long id, SubjectRequest requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var subjectEntity = await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(s => s.SubjectId == id, id);

        subjectEntity.Name = requestDto.Name;
        subjectEntity.NeptunCode = requestDto.NeptunCode;
        subjectEntity.GitHubOrgName = requestDto.GitHubOrgName;

        if (requestDto.Teachers != null)
        {
            var teachers = await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
            var oldSubjectTeachers = await gradeManagementDbContext.SubjectTeacher
                .Where(st => st.SubjectId == subjectEntity.SubjectId)
                .ToListAsync();
            gradeManagementDbContext.SubjectTeacher.RemoveRange(oldSubjectTeachers);
            foreach (var teacher in teachers)
            {
                gradeManagementDbContext.SubjectTeacher.Add(new Dal.Entities.SubjectTeacher
                {
                    SubjectId = subjectEntity.SubjectId,
                    UserId = teacher.Id
                });
            }
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<SubjectResponse>(subjectEntity);
    }

    public async Task<SubjectResponse> CreateAsync(SubjectRequest requestDto)
    {
        var subjectEntity = new Dal.Entities.Subject
        {
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            GitHubOrgName = requestDto.GitHubOrgName,
            Courses = [],
            CiApiKey = requestDto.CiApiKey
        };
        gradeManagementDbContext.Subject.Add(subjectEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        var teachers =
            requestDto.Teachers == null
                ? []
                : await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        foreach (var teacher in teachers)
        {
            gradeManagementDbContext.SubjectTeacher.Add(new Dal.Entities.SubjectTeacher
            {
                SubjectId = subjectEntity.SubjectId,
                UserId = teacher.Id
            });
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<SubjectResponse>(subjectEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var subject = await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(s => s.SubjectId == id, id);
        var subjectTeachers = await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == id)
            .ToListAsync();
        gradeManagementDbContext.SubjectTeacher.RemoveRange(subjectTeachers);
        gradeManagementDbContext.Subject.Remove(subject);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<List<CourseResponse>> GetAllCoursesByIdAsync(long id)
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .Where(c => c.SubjectId == id)
            .ProjectTo<CourseResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        var subjectEntity = await gradeManagementDbContext.Subject
            .Include(s => s.SubjectTeachers).ThenInclude(st => st.User)
            .SingleEntityAsync(s => s.SubjectId == id, id);
        return mapper.Map<List<User>>(subjectEntity.SubjectTeachers.Select(st => st.User));
    }

    public async Task<List<User>> AddTeacherToSubjectByIdAsync(long subjectId, long teacherId)
    {
        var subjectEntity = await gradeManagementDbContext.Subject
            .SingleEntityAsync(s => s.SubjectId == subjectId, subjectId);
        var teacherEntity = await gradeManagementDbContext.User
            .SingleEntityAsync(u => u.Id == teacherId, teacherId);

        gradeManagementDbContext.SubjectTeacher.Add(new Dal.Entities.SubjectTeacher
        {
            SubjectId = subjectEntity.SubjectId,
            UserId = teacherEntity.Id
        });

        await gradeManagementDbContext.SaveChangesAsync();

        return await GetAllTeachersByIdAsync(subjectId);
    }

    public async Task DeleteTeacherFromSubjectByIdAsync(long subjectId, long teacherId)
    {
        object[] keyValues = [subjectId, teacherId];
        var subjectTeacherEntity = await gradeManagementDbContext.SubjectTeacher
            .SingleEntityAsync(st => st.SubjectId == subjectId && st.UserId == teacherId, keyValues);
        gradeManagementDbContext.SubjectTeacher.Remove(subjectTeacherEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<Dal.Entities.Subject> GetModelByIdWithoutQfAsync(long id)
    {
        return await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(s => s.SubjectId == id, id);
    }
}
