using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Octokit;

namespace Ahk.GitHub.Monitor.Services
{
    /// <summary>
    /// Based on https://thomaslevesque.com/2018/03/30/writing-a-github-webhook-as-an-azure-function/.
    /// </summary>
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly IMemoryCache cache;
        private readonly IOptions<GitHubMonitorConfig> config;

        public GitHubClientFactory(IMemoryCache cache, IOptions<GitHubMonitorConfig> config)
        {
            this.cache = cache;
            this.config = config;
        }

        public async Task<IGitHubClient> CreateGitHubClient(long installationId, ILogger logger)
        {
            var token = await cache.GetOrCreateAsync(
                $"githubtokenforinstallation_{installationId}",
                async cacheEntry =>
                {
                    var token = await getInstallationToken(installationId, logger);
                    cacheEntry.SetValue(token);
                    cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    return token;
                });

#pragma warning disable CA2000 // Dispose objects before losing scope
            var httpHandler = new Octokit.Internal.HttpClientAdapter(() => new ResponseLoggerHandler(new RetryOnServerErrorHandler(Octokit.Internal.HttpMessageHandlerFactory.CreateDefault()), logger));
#pragma warning restore CA2000 // Dispose objects before losing scope

            // Connection creation based on what the GitHubClient ctor does
            var githubConnection = new Connection(
                                        new Octokit.ProductHeaderValue("Ahk"),
                                        GitHubClient.GitHubApiUrl,
                                        new Octokit.Internal.InMemoryCredentialStore(new Credentials(token)),
                                        httpHandler,
                                        new Octokit.Internal.SimpleJsonSerializer());

            var gitHubClient = new GitHubClient(githubConnection);
            gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(15));

            return gitHubClient;
        }

        private async Task<string> getInstallationToken(long installationId, ILogger logger)
        {
            var applicationToken = getApplicationToken();
            using (var client = new HttpClient())
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/app/installations/{installationId}/access_tokens")
                {
                    Headers =
                    {
                        Authorization = new AuthenticationHeaderValue("Bearer", applicationToken),
                        UserAgent = { ProductInfoHeaderValue.Parse("Ahk") },
                        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/vnd.github.machine-man-preview+json") },
                    },
                };

                using (var response = await client.SendAsync(request))
                {
                    var json = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                        logger.LogError($"Failed to get access token for installation {installationId}, response payload is: {json}");

                    response.EnsureSuccessStatusCode();

                    var obj = JObject.Parse(json);
                    return obj["token"]?.Value<string>();
                }
            }
        }

        private string getApplicationToken()
        {
            var parameters = CryptoHelper.GetRsaParameters(config.Value.GitHubAppPrivateKey);
            var key = new RsaSecurityKey(parameters);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(
                claims: new[]
                {
                    new Claim("iat", now.ToUnixTimeStamp().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer),
                    new Claim("exp", now.AddMinutes(10).ToUnixTimeStamp().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer),
                    new Claim("iss", config.Value.GitHubAppId, ClaimValueTypes.Integer),
                },
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
