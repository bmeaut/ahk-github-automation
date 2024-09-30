using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Data.Utils;

public static class DbSetExtensions
{
    public static IQueryable<T> IgnoreQueryFiltersButNotIsDeleted<T>(this DbSet<T> dbSet) where T : class
    {
        return dbSet.IgnoreQueryFilters().Where(e => EF.Property<bool>(e, "IsDeleted") == false);
    }
}
