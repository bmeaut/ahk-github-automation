using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Ahk.GradeManagement.Tests.IntegrationTests
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
