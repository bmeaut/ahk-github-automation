using Ahk.GradeManagement.Data;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataServiceBuilder
    {
        public static IServiceCollection AddAhkData(this IServiceCollection services, string cosmosAccountEndpoint, string cosmosAccountKey)
        {
            services.AddScoped(s =>
                new CosmosClient(
                    accountEndpoint: cosmosAccountEndpoint,
                    authKeyOrResourceToken: cosmosAccountKey,
                    clientOptions: new CosmosClientOptions()
                    {
                        // ConnectionMode = ConnectionMode.Gateway,
                        MaxRetryAttemptsOnRateLimitedRequests = 9,
                        SerializerOptions = new CosmosSerializationOptions() { IgnoreNullValues = true, PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
                    }));
            services.AddScoped<IWebhookTokenRepository, Ahk.GradeManagement.Data.Internal.WebhookTokenRepository>();
            services.AddScoped<IResultsRepository, Ahk.GradeManagement.Data.Internal.ResultsRepository>();

            return services;
        }
    }
}
