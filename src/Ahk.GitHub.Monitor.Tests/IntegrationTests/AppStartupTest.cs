using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.IntegrationTests
{
    [TestClass]
    public class AppStartupTest
    {
        [TestMethod]
        public async Task AppStartupSucceeds()
        {
            var startup = new Startup();
            using var host = new HostBuilder()
                                .ConfigureWebJobs(startup.Configure)
                                .Build();
            await host.StartAsync();
        }
    }
}
