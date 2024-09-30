using AutoMapper;
using AutoMapper.QueryableExtensions;

using AutSoft.Linq.Queryable;

using GradeManagement.Bll.Services.BaseServices;
using GradeManagement.Data;
using GradeManagement.Shared.Dtos;

using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Bll.Services;

public class SemesterService(GradeManagementDbContext gradeManagementDbContext, IMapper mapper)
    : IRestrictedCrudServiceBase<Semester>
{
    public async Task<IEnumerable<Semester>> GetAllAsync()
    {
        return await gradeManagementDbContext.Semester
            .ProjectTo<Semester>(mapper.ConfigurationProvider)
            .OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Semester> GetByIdAsync(long id)
    {
        return await gradeManagementDbContext.Semester
            .ProjectTo<Semester>(mapper.ConfigurationProvider)
            .SingleEntityAsync(s => s.Id == id, id);
    }

    public async Task<Semester> CreateAsync(Semester requestDto)
    {
        var semesterEntity = new Data.Models.Semester { Name = requestDto.Name };

        gradeManagementDbContext.Semester.Add(semesterEntity);
        await gradeManagementDbContext.SaveChangesAsync();

        return mapper.Map<Semester>(semesterEntity);
    }

    public async Task DeleteAsync(long id)
    {
        var semesterEntity = await gradeManagementDbContext.Semester.SingleEntityAsync(s => s.Id == id, id);
        gradeManagementDbContext.Semester.Remove(semesterEntity);
        await gradeManagementDbContext.SaveChangesAsync();
    }
}
