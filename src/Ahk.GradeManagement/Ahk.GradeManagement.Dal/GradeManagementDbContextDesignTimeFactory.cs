using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ahk.GradeManagement.Dal;

public class GradeManagementDbContextDesignTimeFactory : IDesignTimeDbContextFactory<GradeManagementDbContext>
{
    public GradeManagementDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GradeManagementDbContext>();
        optionsBuilder.UseSqlServer("Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=GradeManagement.Database;Integrated Security=True");

        return new GradeManagementDbContext(optionsBuilder.Options, null);
    }
}
