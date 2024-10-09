using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Data.Models;
using GradeManagement.Data.Utils;
using GradeManagement.Shared.Dtos.Request;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.EntityFrameworkCore;

using Course = GradeManagement.Shared.Dtos.Course;
using Task = System.Threading.Tasks.Task;
using User = GradeManagement.Shared.Dtos.User;

namespace GradeManagement.Bll.Services;

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
            .SingleEntityAsync(s => s.Id == id, id);

        subjectEntity.Name = requestDto.Name;
        subjectEntity.NeptunCode = requestDto.NeptunCode;
        subjectEntity.GitHubOrgName = requestDto.GitHubOrgName;

        if (requestDto.Teachers != null)
        {
            var teachers = await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
            var oldSubjectTeachers = await gradeManagementDbContext.SubjectTeacher
                .Where(st => st.SubjectId == subjectEntity.Id)
                .ToListAsync();
            gradeManagementDbContext.SubjectTeacher.RemoveRange(oldSubjectTeachers);
            foreach (var teacher in teachers)
            {
                gradeManagementDbContext.SubjectTeacher.Add(new SubjectTeacher
                {
                    SubjectId = subjectEntity.Id, UserId = teacher.Id
                });
            }
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<SubjectResponse>(subjectEntity);
    }

    public async Task<SubjectResponse> CreateAsync(SubjectRequest requestDto)
    {
        var subjectEntity = new Subject
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
            requestDto.Teachers == null ? []
            : await userService.GetAllUserEntitiesFromDtoListAsync(requestDto.Teachers);
        foreach (var teacher in teachers)
        {
            gradeManagementDbContext.SubjectTeacher.Add(new SubjectTeacher
            {
                SubjectId = subjectEntity.Id, UserId = teacher.Id
            });
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<SubjectResponse>(subjectEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var subject = await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(s => s.Id == id, id);
        var subjectTeachers = await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.SubjectId == id)
            .ToListAsync();
        gradeManagementDbContext.SubjectTeacher.RemoveRange(subjectTeachers);
        gradeManagementDbContext.Subject.Remove(subject);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<List<Course>> GetAllCoursesByIdAsync(long id)
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .Where(c => c.SubjectId == id)
            .ProjectTo<Course>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<User>> GetAllTeachersByIdAsync(long id)
    {
        var selctedSubjectEntity = await gradeManagementDbContext.Subject
            .Include(s => s.SubjectTeachers).ThenInclude(st => st.User)
            .Select(s => new { Id = s.Id, SubjectTeachers = s.SubjectTeachers })
            .SingleEntityAsync(s => s.Id == id, id);
        return mapper.Map<List<User>>(selctedSubjectEntity.SubjectTeachers.Select(st => st.User));
    }

    public async Task<List<User>> AddTeacherToSubjectByIdAsync(long subjectId, long teacherId)
    {
        var subjectEntity = await gradeManagementDbContext.Subject
            .SingleEntityAsync(s => s.Id == subjectId, subjectId);
        var teacherEntity = await gradeManagementDbContext.User
            .SingleEntityAsync(u => u.Id == teacherId, teacherId);

        gradeManagementDbContext.SubjectTeacher.Add(new SubjectTeacher
        {
            SubjectId = subjectEntity.Id, UserId = teacherEntity.Id
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

    public async Task<Subject> GetModelByIdWithoutQfAsync(long id)
    {
        return await gradeManagementDbContext.Subject
            .IgnoreQueryFiltersButNotIsDeleted()
            .SingleEntityAsync(s => s.Id == id, id);
    }
}
