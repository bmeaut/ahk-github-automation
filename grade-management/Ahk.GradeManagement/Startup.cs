using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Ahk.GradeManagement.Startup))]

namespace Ahk.GradeManagement
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMemoryCache(setup =>
            {
                setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
            });

            addAzureQueueIntegration(builder);
            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddScoped<ResultProcessing.IResultProcessor, ResultProcessing.ResultProcessor>();
            builder.Services.AddScoped<Services.ITokenManagementService, Services.TokenManagementService>();
            builder.Services.AddScoped<SetGrade.ISetGradeService, SetGrade.SetGradeService>();
            builder.Services.AddScoped<ListGrades.IGradeListing, ListGrades.GradeListing>();

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables("AHK_").Build();
            builder.Services.Configure<AhkAppConfig>(configuration);

            var configValue = new AhkAppConfig();
            configuration.Bind(configValue);
            builder.Services.AddAhkData(configValue.CosmosAccountEndpoint, configValue.CosmosAccountKey);
        }

        private static void addAzureQueueIntegration(IFunctionsHostBuilder builder)
        {
            builder.Services.AddAzureClients(az =>
            {
                az.ConfigureDefaults(opts => opts.Diagnostics.IsLoggingEnabled = false);
                az.AddQueueServiceClient(connectionString: Environment.GetEnvironmentVariable("AHK_EventsQueueConnectionString"))
                    .WithName(SetGrade.SetGradeService.QueueClientName)
                    .ConfigureOptions(options =>
                    {
                        options.MessageEncoding = Azure.Storage.Queues.QueueMessageEncoding.Base64;
                        options.Retry.Mode = Azure.Core.RetryMode.Exponential;
                        options.Retry.MaxRetries = 5;
                        options.Retry.MaxDelay = TimeSpan.FromSeconds(100);
                    });
            });
        }
    }
}
