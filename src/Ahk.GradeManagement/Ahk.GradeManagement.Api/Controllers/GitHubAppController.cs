using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using GradeManagement.Data;
using GradeManagement.Shared.Dtos.GitHubManifest;

using Microsoft.AspNetCore.Mvc;

using System.Net.Http.Headers;
using System.Text.Json;

namespace GradeManagement.Server.Controllers;

[Route("api/github")]
[ApiController]
public class GitHubAppController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> CreateGitHubApp([FromQuery] string code)
    {
        string appUrl = Environment.GetEnvironmentVariable("APP_URL");
        string keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
        var client = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        var httpClient = httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        httpClient.DefaultRequestHeaders.Add("User-Agent", "GradeManagement-App");
        var response = await httpClient.PostAsync($"https://api.github.com/app-manifests/{code}/conversions", null);

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var gitHubApp = JsonSerializer.Deserialize<GitHubApp>(content);
        var pem = RemovePemFencing(gitHubApp.Pem);

        await client.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubAppId",
            gitHubApp.Id.ToString());
        await client.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubAppPrivateKey", pem);
        await client.SetSecretAsync($"GitHubMonitorConfig--{gitHubApp.Owner.Login}--GitHubWebhookSecret",
            gitHubApp.WebhookSecret);

        return Redirect($"https://github.com/organizations/ahk-dev-org/settings/apps/{gitHubApp.Slug}/installations");
    }

    public static string RemovePemFencing(string pem)
    {
        var lines = pem.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var base64String = string.Join("", lines.Skip(1).Take(lines.Length - 2));
        return base64String;
    }
}
