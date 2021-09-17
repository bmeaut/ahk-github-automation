using Ahk.GradeManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataServiceBuilder
    {
        public static IServiceCollection AddAhkData(this IServiceCollection services, string cosmosAccountEndpoint, string cosmosAccountKey)
        {
            services.AddEntityFrameworkCosmos();
            services.AddDbContext<AhkDb>(dbconf =>
            {
                dbconf.UseCosmos(accountEndpoint: cosmosAccountEndpoint, cosmosAccountKey, databaseName: AhkDb.DatabaseName);
            });

            return services;
        }
    }
}
