using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class LanguageService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    : IRestrictedCrudServiceBase<Language>
{
    public async Task<IEnumerable<Language>> GetAllAsync()
    {
        return await gradeManagementDbContext.Language
            .ProjectTo<Language>(mapper.ConfigurationProvider)
            .OrderBy(l => l.Id).ToListAsync();
    }

    public async Task<Language> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Language
            .ProjectTo<Language>(mapper.ConfigurationProvider)
            .SingleEntityAsync(l => l.Id == id, id);
    }

    public async Task<Language> CreateAsync(Language requestDto)
    {
        var languageEntity = new Data.Models.Language();
        languageEntity.Name = requestDto.Name;

        gradeManagementDbContext.Language.Add(languageEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<Language>(languageEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var languageEntity = await gradeManagementDbContext.Language.SingleEntityAsync(l => l.Id == id, id);
        gradeManagementDbContext.Language.Remove(languageEntity);
        gradeManagementDbContext.SaveChanges();
    }
}
