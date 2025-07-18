using Ahk.GradeManagement.Bll.Services.BaseServices;
using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.Shared.Config;
using Ahk.GradeManagement.Shared.Dtos.Request;
using Ahk.GradeManagement.Shared.Dtos.Response;

using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Common.Exceptions;
using AutSoft.Linq.Queryable;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Ahk.GradeManagement.Bll.Services.Utils;
using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Shared.Exceptions;

using Microsoft.EntityFrameworkCore;
using Ahk.GradeManagement.Backend.Common.RequestContext;
using Microsoft.Extensions.Options;
using Ahk.GradeManagement.Backend.Common.Options;

namespace Ahk.GradeManagement.Bll.Services;

public class CourseService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper, IRequestContext requestContext, IOptions<AhkOptions> ahkOptionsAccessor)
    : ICrudServiceBase<CourseRequest, CourseResponse>
{
    private readonly AhkOptions _ahkOptions = ahkOptionsAccessor.Value;

    public async Task<IEnumerable<CourseResponse>> GetAllAsync()
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<CourseResponse>(mapper.ConfigurationProvider)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<CourseResponse> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Course
            .Include(c => c.Semester)
            .Include(c => c.Language)
            .ProjectTo<CourseResponse>(mapper.ConfigurationProvider)
            .SingleEntityAsync(c => c.Id == id, id);
    }

    public async Task<CourseResponse> UpdateAsync(long id, CourseRequest requestDto)
    {
        if (requestDto.Id != id)
        {
            throw new ValidationException("ID", id.ToString(),
                "The Id from the query and the Id of the DTO do not match!");
        }

        var courseEntity = await gradeManagementDbContext.Course
            .SingleEntityAsync(c => c.Id == id, id);
        courseEntity.Name = requestDto.Name;
        courseEntity.MoodleClientId = requestDto.MoodleClientId;
        courseEntity.SubjectId = requestDto.SubjectId;
        courseEntity.SemesterId = requestDto.Semester.Id;
        courseEntity.LanguageId = requestDto.Language.Id;

        await gradeManagementDbContext.SaveChangesAsync();

        return await GetByIdAsync(courseEntity.Id);
    }

    public async Task<CourseResponse> CreateAsync(CourseRequest requestDto)
    {
        if (requestDto.SubjectId != requestContext.CurrentUser?.CurrentSubjectId)
            throw new UnauthorizedException("Current subject does not match the subject of the course!");

        var keyGenerator = new RsaKeyGenerator();

        var courseEntityToBeCreated = new Course
        {
            Id = requestDto.Id,
            Name = requestDto.Name,
            MoodleClientId = requestDto.MoodleClientId,
            PublicKey = keyGenerator.PublicKey,
            SubjectId = requestDto.SubjectId,
            SemesterId = requestDto.Semester.Id,
            LanguageId = requestDto.Language.Id,
        };
        gradeManagementDbContext.Course.Add(courseEntityToBeCreated);
        await gradeManagementDbContext.SaveChangesAsync();

        await SetSecret(requestDto.MoodleClientId, keyGenerator.PrivateKey);

        return await GetByIdAsync(courseEntityToBeCreated.Id);
    }

    public async Task DeleteAsync(long id)
    {
        var courseEntity = await gradeManagementDbContext.Course.SingleEntityAsync(c => c.Id == id, id);
        gradeManagementDbContext.Course.Remove(courseEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }

    public async Task<IEnumerable<ExerciseResponse>> GetAllExercisesByIdAsync(long id)
    {
        var exercises = await gradeManagementDbContext.Exercise
            .Where(e => e.CourseId == id)
            .Include(e => e.ScoreTypeExercises).ThenInclude(ste => ste.ScoreType)
            .ToListAsync();
        return mapper.Map<List<ExerciseResponse>>(exercises);
    }

    public async Task<IEnumerable<GroupResponse>> GetAllGroupsByIdAsync(long id)
    {
        return await gradeManagementDbContext.Group
            .Where(g => g.CourseId == id)
            .ProjectTo<GroupResponse>(mapper.ConfigurationProvider)
            .ToListAsync();
    }

    private async Task SetSecret(string moodleClientId, string privateKey)
    {
        var client = new SecretClient(new Uri(_ahkOptions.KeyVaultUrl), new DefaultAzureCredential());

        await client.SetSecretAsync($"{MoodleConfig.Name}--{moodleClientId}--MoodlePrivateKey", privateKey);
    }
}
