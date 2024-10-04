using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;
using GradeManagement.Shared.Enums;

using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

namespace GradeManagement.Bll.Services;

public class UserService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    : ICrudServiceBase<User>
{
    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await gradeManagementDbContext.User
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<User> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.User
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .SingleEntityAsync(u => u.Id == id, id);
    }

    public async Task<User> CreateAsync(User requestDto)
    {
        var userEntity = new Data.Models.User
        {
            Name = requestDto.Name,
            NeptunCode = requestDto.NeptunCode,
            GithubId = requestDto.GithubId,
            BmeEmail = requestDto.BmeEmail,
            Type = requestDto.Type
        };
        gradeManagementDbContext.User.Add(userEntity);
        await gradeManagementDbContext.SaveChangesAsync();
        return mapper.Map<User>(userEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var userEntity = await gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        var groupTeachers = await gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.UserId == id)
            .ToListAsync();
        var subjectTeachers = await gradeManagementDbContext.SubjectTeacher
            .Where(st => st.UserId == id)
            .ToListAsync();
        gradeManagementDbContext.SubjectTeacher.RemoveRange(subjectTeachers);
        gradeManagementDbContext.GroupTeacher.RemoveRange(groupTeachers);
        gradeManagementDbContext.User.Remove(userEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<User> UpdateAsync(long id, User requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var userEntity = await gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        userEntity.Name = requestDto.Name;
        userEntity.NeptunCode = requestDto.NeptunCode;
        userEntity.GithubId = requestDto.GithubId;
        userEntity.BmeEmail = requestDto.BmeEmail;
        userEntity.Type = requestDto.Type;

        await gradeManagementDbContext.SaveChangesAsync();
        return mapper.Map<User>(userEntity);
    }

    public async Task<List<GroupResponse>> GetAllGroupsByIdAsync(long id)
    {
        return await gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.GroupTeachers)
            .Select(gt => gt.Group)
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<SubjectResponse>> GetAllSubjectsByIdAsync(long id)
    {
        return await gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.SubjectTeachers)
            .Select(st => st.Subject)
            .ProjectTo<SubjectResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Data.Models.User>> GetAllUserEntitiesFromDtoListAsync(List<User> users)
    {
        var userEntities = await gradeManagementDbContext.User
            .Where(t => users.Select(rqT => rqT.Id).Contains(t.Id))
            .ToListAsync();
        if (userEntities.Count != users.Count)
        {
            //Select teachers not found
            var teachersNotFoundIds =
                users.Where(rqT => userEntities.All(t => t.Id != rqT.Id)).Select(t => t.Id).ToList();
            foreach (var teacherId in teachersNotFoundIds)
            {
                throw EntityNotFoundException.CreateForType<Data.Models.User>(teacherId);
            }
        }

        return userEntities;
    }

    public async Task<List<PullRequest>> GetAllPullRequestsByIdAsync(long id)
    {
        return await gradeManagementDbContext.PullRequest
            .Where(pr => pr.TeacherId == id)
            .ProjectTo<PullRequest>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Data.Models.User> GetModelByGitHubIdAsync(string githubId)
    {
        return await gradeManagementDbContext.User
            .SingleEntityAsync(u => u.GithubId == githubId, 0);
    }

    public async Task<Data.Models.User> GetOrCreateUserByEmailAsync(string email)
    {
        var user = await gradeManagementDbContext.User.Where(u => u.BmeEmail == email).FirstOrDefaultAsync();
        if (user != null) return user;
        user = new Data.Models.User
        {
            Name = "Auto Generated from email: " + email,
            NeptunCode = "",
            GithubId = "",
            BmeEmail = email,
            Type = UserType.User
        };
        gradeManagementDbContext.User.Add(user);
        await gradeManagementDbContext.SaveChangesAsync();
        return user;
    }

    public async Task<User> GetCurrentUserAsync(ClaimsPrincipal httpContextUser)
    {
        var email = AuthorizationHelper.GetCurrentUserEmail(httpContextUser);
        var currentUser = await gradeManagementDbContext.User
            .ProjectTo<User>(mapper.ConfigurationProvider)
            .SingleEntityAsync(u => u.BmeEmail == email, 0);
        return currentUser;
    }
}
