using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    [TestClass]
    public class AppStartupTest
    {
        [TestMethod]
        public async Task AppStartupSucceeds()
        {
            var host = new HostBuilder()
                .ConfigureServices(services =>
                {
                    // Register any services that are needed for the test
                    services.AddSingleton<Services.IGitHubClientFactory, Services.GitHubClientFactory>();
                    // Add more service registrations as needed
                })
                .Build();

            await host.StartAsync();

            // Additional assertions or operations can be performed here

            await host.StopAsync();
        }
    }
}
