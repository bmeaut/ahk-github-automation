using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;
using GradeManagement.Shared.Dtos.Response;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class UserService : ICrudServiceBase<User>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public UserService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _gradeManagementDbContext.User
            .ProjectTo<User>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<User> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .ProjectTo<User>(_mapper.ConfigurationProvider)
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
        _gradeManagementDbContext.User.Add(userEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<User>(userEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var userEntity = await _gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        var groupTeachers = await _gradeManagementDbContext.GroupTeacher
            .Where(gt => gt.UserId == id)
            .ToListAsync();
        var subjectTeachers = await _gradeManagementDbContext.SubjectTeacher
            .Where(st => st.UserId == id)
            .ToListAsync();
        _gradeManagementDbContext.SubjectTeacher.RemoveRange(subjectTeachers);
        _gradeManagementDbContext.GroupTeacher.RemoveRange(groupTeachers);
        _gradeManagementDbContext.User.Remove(userEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<User> UpdateAsync(long id, User requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var userEntity = await _gradeManagementDbContext.User.SingleEntityAsync(u => u.Id == id, id);
        userEntity.Name = requestDto.Name;
        userEntity.NeptunCode = requestDto.NeptunCode;
        userEntity.GithubId = requestDto.GithubId;
        userEntity.BmeEmail = requestDto.BmeEmail;
        userEntity.Type = requestDto.Type;

        await _gradeManagementDbContext.SaveChangesAsync();
        return _mapper.Map<User>(userEntity);
    }

    public async Task<List<Group>> GetAllGroupsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.GroupTeachers)
            .Select(gt => gt.Group)
            .ProjectTo<Group>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Subject>> GetAllSubjectsByIdAsync(long id)
    {
        return await _gradeManagementDbContext.User
            .Where(u => u.Id == id)
            .SelectMany(u => u.SubjectTeachers)
            .Select(st => st.Subject)
            .ProjectTo<Subject>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<List<Data.Models.User>> GetAllUserEntitiesFromDtoListAsync(List<User> users)
    {
        var userEntities = await _gradeManagementDbContext.User
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
        return await _gradeManagementDbContext.PullRequest
            .Where(pr => pr.TeacherId == id)
            .ProjectTo<PullRequest>(_mapper.ConfigurationProvider)
            .ToListAsync();
    }

    public async Task<Data.Models.User> GetModelByGitHubIdAsync(string githubId)
    {
        return await _gradeManagementDbContext.User
            .SingleEntityAsync(u => u.GithubId == githubId, 0);
    }
}
