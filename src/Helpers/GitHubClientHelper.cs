using Ahk.GitHub.Monitor.Extensions;
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

namespace Ahk.GitHub.Monitor
{
    /// <summary>
    /// Based on https://thomaslevesque.com/2018/03/30/writing-a-github-webhook-as-an-azure-function/
    /// </summary>
    public static class GitHubClientHelper
    {
        private static readonly IMemoryCache connectionCache = new MemoryCache(Options.Create(new MemoryCacheOptions() { ExpirationScanFrequency = TimeSpan.FromMinutes(3) }));

        public static Task<GitHubClient> CreateGitHubClient(long installationId)
            => connectionCache.GetOrCreateAsync(installationId, cacheEntry => CreateNewGitHubClient(cacheEntry, installationId));

        private static async Task<GitHubClient> CreateNewGitHubClient(ICacheEntry cacheEntry, long installationId)
        {
            var gitHubClient = new GitHubClient(new Octokit.ProductHeaderValue("Ahk"))
            {
                Credentials = new Credentials(await GetInstallationToken(installationId))
            };
            gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(15));

            cacheEntry.SetValue(gitHubClient);
            cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            return gitHubClient;
        }

        private static async Task<string> GetInstallationToken(long installationId)
        {
            var applicationToken = GetApplicationToken();
            using (var client = new HttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.github.com/installations/{installationId}/access_tokens")
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

        private static string GetApplicationToken()
        {
            var parameters = CryptoHelper.GetRsaParameters(Environment.GetEnvironmentVariable("AHK_GITHUB_APP_PRIVATE_KEY", EnvironmentVariableTarget.Process));
            var key = new RsaSecurityKey(parameters);
            var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
            var now = DateTime.UtcNow;
            var token = new JwtSecurityToken(claims: new[]
            {
                new Claim("iat", now.ToUnixTimeStamp().ToString(), ClaimValueTypes.Integer),
                new Claim("exp", now.AddMinutes(10).ToUnixTimeStamp().ToString(), ClaimValueTypes.Integer),
                new Claim("iss", Environment.GetEnvironmentVariable("AHK_GITHUB_APP_ID", EnvironmentVariableTarget.Process), ClaimValueTypes.Integer)
            },
            signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
