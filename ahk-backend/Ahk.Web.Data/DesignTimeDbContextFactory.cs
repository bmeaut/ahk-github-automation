using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ahk.Web.Data;

/// <summary>
/// Lets <c>dotnet ef</c> build the context directly from this project (no web host needed). Uses a
/// LocalDB design-time connection; the runtime connection string comes from configuration in the web app.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=AhkWeb;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new ApplicationDbContext(options, new NullCurrentCourseProvider());
    }
}
