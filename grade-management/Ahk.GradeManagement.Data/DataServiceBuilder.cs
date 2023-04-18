using Ahk.GradeManagement.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataServiceBuilder
    {
        public static IServiceCollection AddAhkData(this IServiceCollection services)
        {

            //services.AddScoped<IWebhookTokenRepository, Ahk.GradeManagement.Data.Internal.WebhookTokenRepository>();
            //services.AddScoped<IResultsRepository, Ahk.GradeManagement.Data.Internal.ResultsRepository>();
            //services.AddScoped<IStatusTrackingRepository, Ahk.GradeManagement.Data.Internal.StatusTrackingRepository>();

            return services;
        }
    }
}
