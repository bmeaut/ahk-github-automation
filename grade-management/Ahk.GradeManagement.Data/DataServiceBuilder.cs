using Ahk.GradeManagement.Data;
using Microsoft.Azure.Cosmos;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DataServiceBuilder
    {
        public static IServiceCollection AddAhkData(this IServiceCollection services, string cosmosAccountEndpoint, string cosmosAccountKey)
        {
            var serializerSettings = new Newtonsoft.Json.JsonSerializerSettings
            {
                Converters =
                {
                    new Ahk.GradeManagement.Data.Helper.StatusEventItemJsonConverter(),
                },
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver
                {
                    NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy()
                },
            };

            services.AddScoped(s =>
                new CosmosClient(
                    accountEndpoint: cosmosAccountEndpoint,
                    authKeyOrResourceToken: cosmosAccountKey,
                    clientOptions: new CosmosClientOptions()
                    {
#if DEBUG
                        ConnectionMode = ConnectionMode.Gateway,
#endif
                        MaxRetryAttemptsOnRateLimitedRequests = 9,
                        Serializer = new Ahk.GradeManagement.Data.Helper.NewtonsoftJsonCosmosSerializer(serializerSettings),
                    }));

            services.AddScoped<IWebhookTokenRepository, Ahk.GradeManagement.Data.Internal.WebhookTokenRepository>();
            services.AddScoped<IResultsRepository, Ahk.GradeManagement.Data.Internal.ResultsRepository>();
            services.AddScoped<IStatusTrackingRepository, Ahk.GradeManagement.Data.Internal.StatusTrackingRepository>();

            return services;
        }
    }
}
