using Microsoft.Azure.Cosmos;
using Ahk.GradeManagement.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataServiceBuilder
    {
        public static IServiceCollection AddAhkData(this IServiceCollection services, string cosmosAccountEndpoint, string cosmosAccountKey)
        {
            services.AddSingleton(s =>
                new CosmosClient(
                    accountEndpoint: cosmosAccountEndpoint,
                    authKeyOrResourceToken: cosmosAccountKey,
                    clientOptions: new CosmosClientOptions()
                    {
                        // ConnectionMode = ConnectionMode.Gateway,
                        MaxRetryAttemptsOnRateLimitedRequests = 9,
                        SerializerOptions = new CosmosSerializationOptions() { IgnoreNullValues = true, PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase }
                    }));
            services.AddSingleton<IWebhookTokenRepository, Ahk.GradeManagement.Data.Internal.WebhookTokenRepository>();
            services.AddSingleton<IResultsRepository, Ahk.GradeManagement.Data.Internal.ResultsRepository>();

            return services;
        }
    }
}
