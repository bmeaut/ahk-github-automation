using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

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

            builder.Services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
            builder.Services.AddScoped<ResultProcessing.IResultProcessor, ResultProcessing.ResultProcessor>();
            builder.Services.AddScoped<Services.ITokenManagementService, Services.TokenManagementService>();
            builder.Services.AddScoped<SetGrade.ISetGradeService, SetGrade.SetGradeService>();
            builder.Services.AddScoped<ListGrades.IGradeListing, ListGrades.GradeListing>();

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables("AHK_").Build();
            builder.Services.Configure<AppConfig>(configuration);

            var configValue = new AppConfig();
            configuration.Bind(configValue);
            builder.Services.AddAhkData(configValue.CosmosAccountEndpoint, configValue.CosmosAccountKey);
        }
    }
}