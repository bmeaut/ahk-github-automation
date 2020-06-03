using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Ahk.GitHub.Monitor.Startup))]

namespace Ahk.GitHub.Monitor
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddMemoryCache(setup =>
            {
                setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
            });
            builder.Services.AddSingleton<Services.IGitHubClientFactory, Services.GitHubClientFactory>();

            var configuration = new ConfigurationBuilder().AddEnvironmentVariables("AHK_").Build();
            builder.Services.Configure<GitHubMonitorConfig>(configuration);
        }
    }
}