using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Octokit;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.Services
{
    /// <summary>
    /// Based on https://thomaslevesque.com/2018/03/30/writing-a-github-webhook-as-an-azure-function/
    /// </summary>
    public class GitHubClientFactory : IGitHubClientFactory
    {
        private readonly IMemoryCache connectionCache;
        private readonly IOptions<GitHubMonitorConfig> config;

        public GitHubClientFactory(IMemoryCache connectionCache, IOptions<GitHubMonitorConfig> config)
        {
            this.connectionCache = connectionCache;
            this.config = config;
        }

        public Task<IGitHubClient> CreateGitHubClient(long installationId)
            => connectionCache.GetOrCreateAsync($"githubconn_{installationId}", cacheEntry => createNewGitHubClient(cacheEntry, installationId));

        private async Task<IGitHubClient> createNewGitHubClient(ICacheEntry cacheEntry, long installationId)
        {
            var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("Ahk"))
            {
                Credentials = new Credentials(await getInstallationToken(installationId))
            };
            gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(15));

            cacheEntry.SetValue(gitHubClient);
            cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            return gitHubClient;
        }

        private async Task<string> getInstallationToken(long installationId)
        {
            var applicationToken = getApplicationToken();
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/app/installations/{installationId}/access_tokens")
                {
                    Headers =
                    {
                        Authorization = new AuthenticationHeaderValue("Bearer", applicationToken),
                        UserAgent =
                        {
                            ProductInfoHeaderValue.Parse("Ahk"),
                        },
                        Accept =
                        {
                            MediaTypeWithQualityHeaderValue.Parse("application/vnd.github.machine-man-preview+json")
                        }
                    }
                };

                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    var json = await response.Content.ReadAsStringAsync();
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
            var token = new JwtSecurityToken(claims: new[]
            {
                new Claim("iat", now.ToUnixTimeStamp().ToString(), ClaimValueTypes.Integer),
                new Claim("exp", now.AddMinutes(10).ToUnixTimeStamp().ToString(), ClaimValueTypes.Integer),
                new Claim("iss", config.Value.GitHubAppId, ClaimValueTypes.Integer)
            },
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
