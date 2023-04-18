using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ahk.GradeManagement.Data
{
    public class AhkDbContextFactory : IDesignTimeDbContextFactory<AhkDbContext>
    {
        public AhkDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AhkDbContext>();
            optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("SQLAZURECONNSTR_AHK_ConnString"),
                sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure();
                });

            return new AhkDbContext(optionsBuilder.Options);
        }
    }
}
