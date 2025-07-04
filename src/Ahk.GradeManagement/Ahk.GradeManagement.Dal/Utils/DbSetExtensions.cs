using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Data.Utils;

public static class DbSetExtensions
{
    public static IQueryable<T> IgnoreQueryFiltersButNotIsDeleted<T>(this DbSet<T> dbSet)
        where T : class, ISoftDelete
    {
        return dbSet.IgnoreQueryFilters().Where(e => !e.IsDeleted);
    }
}
