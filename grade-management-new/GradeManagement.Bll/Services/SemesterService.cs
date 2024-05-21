using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class SemesterService : IRestrictedCrudServiceBase<Semester>
{

    private readonly GradeManagementDbContext _gradeManagementDbContext;
    private readonly IMapper _mapper;

    public SemesterService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    {
        _gradeManagementDbContext = gradeManagementDbContext;
        _mapper = mapper;
    }

    public async Task<IEnumerable<Semester>> GetAllAsync()
    {
        return await _gradeManagementDbContext.Semester
            .ProjectTo<Semester>(_mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Semester> GetByIdAsync(long id)
    {
        return await _gradeManagementDbContext.Semester
            .ProjectTo<Semester>(_mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Semester> CreateAsync(Semester requestDto)
    {
        var semesterEntity = new Data.Models.Semester();
        semesterEntity.Name = requestDto.Name;

        _gradeManagementDbContext.Semester.Add(semesterEntity);
        await _gradeManagementDbContext.SaveChangesAsync();

        return _mapper.Map<Semester>(semesterEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var semesterEntity = await _gradeManagementDbContext.Semester.SingleEntityAsync(s => s.Id == id, id);
        _gradeManagementDbContext.Semester.Remove(semesterEntity);
        await _gradeManagementDbContext.SaveChangesAsync();
    }
}
