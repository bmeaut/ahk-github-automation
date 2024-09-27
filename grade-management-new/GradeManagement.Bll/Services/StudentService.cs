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
using Microsoft.IdentityModel.Tokens;

using Assignment = GradeManagement.Shared.Dtos.Assignment;

namespace GradeManagement.Bll.Services;

public class StudentService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    : ICrudServiceBase<StudentRequest, StudentResponse>
{
    public async Task<IEnumerable<StudentResponse>> GetAllAsync()
    {
        return await gradeManagementDbContext.Student
            .ProjectTo<StudentResponse>(mapper.ConfigurationProvider)
            .OrderBy(s => s.Id)
            .ToListAsync();
    }

    public async Task<StudentResponse> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Student
            .ProjectTo<StudentResponse>(mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<StudentResponse> CreateAsync(StudentRequest requestDto)
    {
        var studentEntity = new Student()
        {
            Name = requestDto.Name, NeptunCode = requestDto.NeptunCode, GithubId = requestDto.GithubId
        };

        gradeManagementDbContext.Student.Add(studentEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        if (!requestDto.GroupIds.IsNullOrEmpty())
        {
            foreach (var groupId in requestDto.GroupIds)
            {
                var group = await gradeManagementDbContext.Group.SingleEntityAsync(g => g.Id == groupId, groupId);
                gradeManagementDbContext.GroupStudent.Add(
                    new GroupStudent { GroupId = group.Id, StudentId = studentEntity.Id });
            }
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(studentEntity.Id);
    }

    public async Task<StudentResponse> UpdateAsync(long id, StudentRequest requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var studentEntity = await gradeManagementDbContext.Student.SingleEntityAsync(s => s.Id == id, id);
        studentEntity.Name = requestDto.Name;
        studentEntity.NeptunCode = requestDto.NeptunCode;
        studentEntity.GithubId = requestDto.GithubId;

        gradeManagementDbContext.GroupStudent.RemoveRange(
            await gradeManagementDbContext.GroupStudent.Where(gs => gs.StudentId == id).ToListAsync());
        if (!requestDto.GroupIds.IsNullOrEmpty())
        {
            foreach (var groupId in requestDto.GroupIds)
            {
                var group = await gradeManagementDbContext.Group.SingleEntityAsync(g => g.Id == groupId, groupId);
                gradeManagementDbContext.GroupStudent.Add(
                    new GroupStudent { GroupId = group.Id, StudentId = studentEntity.Id });
            }
        }

        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(studentEntity.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var studentEntity = await gradeManagementDbContext.Student.SingleEntityAsync(s => s.Id == id, id);
        var groupStudents = await gradeManagementDbContext.GroupStudent.Where(gs => gs.StudentId == id).ToListAsync();
        gradeManagementDbContext.GroupStudent.RemoveRange(groupStudents);
        gradeManagementDbContext.Student.Remove(studentEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<List<GroupResponse>> GetAllGroupsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Student
            .Where(s => s.Id == id)
            .SelectMany(s => s.GroupStudents)
            .Select(gt => gt.Group)
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Assignment>> GetAllAssignmentsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Assignment
            .Where(a => a.StudentId == id)
            .ProjectTo<Assignment>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Student> GetOrCreateStudentByGitHubIdAsync(string studentGitHubId)
    {
        var student = await gradeManagementDbContext.Student
            .SingleOrDefaultAsync(s => s.GithubId == studentGitHubId);

        if (student != null) return student;

        student = new Student { Name = "Auto created from GitHub ID: " + studentGitHubId, GithubId = studentGitHubId };
        gradeManagementDbContext.Student.Add(student);
        await gradeManagementDbContext.SaveChangesAsync();

        return student;
    }

    public async Task<Student> GetStudentModelByGitHubIdAsync(string studentGitHubId)
    {
        return await gradeManagementDbContext.Student.SingleEntityAsync(s => s.GithubId == studentGitHubId, 0);
    }

    public async Task<Student> GetStudentModelByNeptunAsync(string studentNeptun)
    {
        var student = await gradeManagementDbContext.Student.SingleOrDefaultAsync(s => s.NeptunCode == studentNeptun);
        if (student == null)
        {
            //TODO: Send message to student through GitHub that the entered Neptun code is not valid
            throw new ValidationException("NeptunCode", studentNeptun,
                "StudentRequest not found with this Neptun code!");
        }

        return student;
    }
}
