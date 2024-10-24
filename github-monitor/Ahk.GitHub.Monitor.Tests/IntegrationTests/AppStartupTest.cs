using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;
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
                    services.AddSingleton<Services.IGitHubClientFactory, GitHubClientFactory>();
                })
                .Build();

            await host.StartAsync();

            await host.StopAsync();
        }
    }
}
