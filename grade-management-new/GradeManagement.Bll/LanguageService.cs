using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll;

public class LanguageService : IRestrictedCrudServiceBase<Language>
{
    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public LanguageService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Language>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Language
            .ProjectTo<Language>(_mapper.ConfigurationProvider)
            .OrderBy(l => l.Id).ToListAsync();
    }

    public async Task<Language> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Language
            .ProjectTo<Language>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(l => l.Id == id, id);
    }

    public async Task<Language> CreateAsync(Language requestDto)
    {
        var languageEntity = new Data.Models.Language();
        languageEntity.Name = requestDto.Name;

        _gradeManagementDbContext.Language.Add(languageEntity);
        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Language>(languageEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var languageEntity = await _gradeManagementDbContext.Language.SingleEntityAsync(l => l.Id == id, id);
        _gradeManagementDbContext.Language.Remove(languageEntity);
        _gradeManagementDbContext.SaveChanges();
    }
}
