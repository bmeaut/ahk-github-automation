using System;
using Ahk.Lifecycle.Management.DAL;
using Ahk.Lifecycle.Management.DAL.Entities;
using Ahk.Lifecycle.Management.DAL.Helper;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

[assembly: FunctionsStartup(typeof(Ahk.Lifecycle.Management.Startup))]

namespace Ahk.Lifecycle.Management
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddScoped<CosmosClient>(s => createCosmosClient());
            builder.Services.AddScoped<IRepository, CosmosDbRepository>();
        }

        private static CosmosClient createCosmosClient()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                Converters =
                {
                    new LifecycleEventItemJsonConverter(),
                },
            };

            return
                new CosmosClient(
                    connectionString: Environment.GetEnvironmentVariable("AHK_CosmosDbConnectionString"),
                    clientOptions: new CosmosClientOptions()
                    {
                        Serializer = new NewtonsoftJsonCosmosSerializer(serializerSettings),
                    });
        }
    }
}
