using System;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Ahk.GitHub.Monitor.Config;
using Ahk.GitHub.Monitor.Extensions;
using Ahk.GitHub.Monitor.Helpers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Octokit;
using Octokit.Internal;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace Ahk.GitHub.Monitor.Services.GitHubClientFactory;

/// <summary>
/// Based on https://thomaslevesque.com/2018/03/30/writing-a-github-webhook-as-an-azure-function/.
/// </summary>
public class GitHubClientFactory(IMemoryCache cache, IConfiguration configuration) : IGitHubClientFactory
{
    public async Task<IGitHubClient> CreateGitHubClient(string gitHubOrg, long installationId, ILogger logger)
    {
        var token = await cache.GetOrCreateAsync(
            $"githubtokenforinstallation_{installationId}",
            async cacheEntry =>
            {
                var token = await this.getInstallationToken(gitHubOrg, installationId, logger);
                cacheEntry.SetValue(token);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                return token;
            });

#pragma warning disable CA2000 // Dispose objects before losing scope
        var httpHandler = new HttpClientAdapter(() =>
            new ResponseLoggerHandler(
                new RetryOnServerErrorHandler(HttpMessageHandlerFactory.CreateDefault()), logger));
#pragma warning restore CA2000 // Dispose objects before losing scope

        // Connection creation based on what the GitHubClient ctor does
        var githubConnection = new Connection(
            new ProductHeaderValue("Ahk"),
            GitHubClient.GitHubApiUrl,
            new InMemoryCredentialStore(new Credentials(token)),
            httpHandler,
            new SimpleJsonSerializer());

        var gitHubClient = new GitHubClient(githubConnection);
        gitHubClient.SetRequestTimeout(TimeSpan.FromSeconds(15));

        return gitHubClient;
    }

    private async Task<string> getInstallationToken(string gitHubOrg, long installationId, ILogger logger)
    {
        var applicationToken = this.getApplicationToken(gitHubOrg);
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.github.com/app/installations/{installationId}/access_tokens");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", applicationToken);
        request.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse("Ahk"));
        request.Headers.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse("application/vnd.github.machine-man-preview+json"));

        using HttpResponseMessage response = await client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                $"Failed to get access token for installation {installationId}, response payload is: {json}");
        }

        response.EnsureSuccessStatusCode();

        var obj = JObject.Parse(json);
        return obj["token"]?.Value<string>();
    }

    private string getApplicationToken(string gitHubOrg)
    {
        var orgConfig = new GitHubMonitorConfig();
        configuration.GetSection(GitHubMonitorConfig.GetSectionName(gitHubOrg)).Bind(orgConfig);

        RSAParameters parameters = CryptoHelper.GetRsaParameters(orgConfig.GitHubAppPrivateKey);
        var key = new RsaSecurityKey(parameters);
        var creds = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        DateTime now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("iat", now.ToUnixTimeStamp().ToString(CultureInfo.InvariantCulture),
                    ClaimValueTypes.Integer),
                new Claim("exp",
                    now.AddMinutes(10).ToUnixTimeStamp()
                        .ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer),
                new Claim("iss", orgConfig.GitHubAppId, ClaimValueTypes.Integer)
            ],
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
