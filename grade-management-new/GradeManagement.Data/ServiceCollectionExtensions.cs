using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GradeManagement.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGradeManagementDbContext(this IServiceCollection services, IConfiguration configuration, string connectionStringName)
    {
        services.AddDbContext<GradeManagementDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(connectionStringName);
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
