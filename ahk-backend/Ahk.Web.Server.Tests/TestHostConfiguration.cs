using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ahk.Web.Server.Tests;

internal static class TestHostConfiguration
{
    /// <summary>
    /// Turns off the webhook delivery worker.
    ///
    /// <para>⚠️ Every test host here boots the real <c>Program</c>, so without this the background worker
    /// starts and polls the in-memory database every couple of seconds for the whole run — in every test
    /// class, forever. Only the drain test wants it, and it turns it back on for itself.</para>
    /// </summary>
    public static IHostBuilder WithoutWebhookWorker(this IHostBuilder builder) =>
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Webhooks:WorkerEnabled"] = "false",
        }));
}
