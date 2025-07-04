using Ahk.GradeManagement.Dal.Entities.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace Ahk.GradeManagement.Dal.Extensions;

public static class DbSetExtensions
{
    public static IQueryable<T> IgnoreQueryFiltersButNotIsDeleted<T>(this DbSet<T> dbSet)
        where T : class, ISoftDelete
    {
        return dbSet.IgnoreQueryFilters().Where(e => !e.IsDeleted);
    }
}
