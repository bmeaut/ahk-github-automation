using Ahk.GradeManagement.Backend.Common.Options;
using Ahk.GradeManagement.Shared.Dtos.GitHubManifest;

using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using System.Net.Http.Headers;
using System.Text.Json;

namespace Ahk.GradeManagement.Api.Controllers;

[Route("api/github")]
[ApiController]
public class GitHubAppController(IHttpClientFactory httpClientFactory, SecretClient secretClient) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> CreateGitHubApp([FromQuery] string code)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        httpClient.DefaultRequestHeaders.Add("User-Agent", "GradeManagement-App");
        var response = await httpClient.PostAsync($"https://api.github.com/app-manifests/{code}/conversions", null);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var gitHubApp = JsonSerializer.Deserialize<GitHubApp>(content);

        var lines = gitHubApp.Pem.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        var pem = string.Join("", lines.Skip(1).Take(lines.Length - 2));

        await secretClient.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubAppId", gitHubApp.Id.ToString());
        await secretClient.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubAppPrivateKey", pem);
        await secretClient.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubWebhookSecret", gitHubApp.WebhookSecret);

        return Redirect($"https://github.com/organizations/ahk-dev-org/settings/apps/{gitHubApp.Slug}/installations");
    }
}
