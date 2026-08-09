using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ahk.Web.Services.GitHub;

/// <summary>An installation access token plus the permissions GitHub actually granted it.</summary>
public sealed record GitHubInstallationToken(string Token, long InstallationId, IReadOnlyDictionary<string, string> Permissions, string? RepositorySelection)
{
    /// <summary>
    /// True when the installation may create repositories, add collaborators and change repository settings —
    /// everything the assignment flow does. Reported by the health check so an administrator sees the gap
    /// before a student walks into a 403.
    /// </summary>
    public bool HasAdministrationWrite =>
        Permissions.TryGetValue("administration", out var level) && string.Equals(level, "write", StringComparison.Ordinal);
}

public interface ICourseGitHubAppTokenProvider
{
    /// <summary>
    /// Mints (or returns a cached) installation token for the course's organization. Returns null — never
    /// throws — when the course has no GitHub App configured or no organization, so callers can turn that into
    /// a clean "this course is not connected to GitHub yet" message.
    /// </summary>
    Task<GitHubInstallationToken?> GetForCourseAsync(int courseId, bool bypassCache = false, CancellationToken cancellationToken = default);

    /// <summary>Overload for callers that already loaded the course with its <see cref="CourseGitHubConfig"/>.</summary>
    Task<GitHubInstallationToken?> GetForCourseAsync(Course course, bool bypassCache = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Turns a course's GitHub App credentials into an installation access token, the identity every write the
/// portal performs on GitHub runs as. Port of the flow in
/// <c>github-monitor/.../GitHubClientFactory.cs</c>, with two differences: the installation is looked up from
/// the organization (there is no webhook payload to read it from), and the JWT is built with
/// <see cref="RSA.ImportFromPem(ReadOnlySpan{char})"/> rather than the hand-rolled DER reader that predates it.
/// </summary>
public sealed class CourseGitHubAppTokenProvider : ICourseGitHubAppTokenProvider
{
    /// <summary>GitHub issues installation tokens for 60 minutes; renew with room to spare.</summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(50);

    private readonly ApplicationDbContext db;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IMemoryCache cache;

    public CourseGitHubAppTokenProvider(ApplicationDbContext db, IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        this.db = db;
        this.httpClientFactory = httpClientFactory;
        this.cache = cache;
    }

    public async Task<GitHubInstallationToken?> GetForCourseAsync(int courseId, bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        // Course itself is not course-scoped, but the include is, so this is a plain lookup by primary key.
        var course = await db.Courses
            .AsNoTracking()
            .Include(c => c.GitHubConfig)
            .FirstOrDefaultAsync(c => c.Id == courseId, cancellationToken);

        return course is null ? null : await GetForCourseAsync(course, bypassCache, cancellationToken);
    }

    public async Task<GitHubInstallationToken?> GetForCourseAsync(Course course, bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(course);

        var appId = course.GitHubConfig?.GitHubAppId;
        var privateKey = course.GitHubConfig?.GitHubAppPrivateKey;
        var organization = course.GitHubOrganization;

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(organization))
            return null;

        var key = $"githubinstallationtoken_{course.Id}";
        if (!bypassCache && cache.TryGetValue<GitHubInstallationToken>(key, out var cached) && cached is not null)
            return cached;

        var token = await MintAsync(appId, privateKey, organization, cancellationToken);
        cache.Set(key, token, CacheDuration);
        return token;
    }

    private async Task<GitHubInstallationToken> MintAsync(string appId, string privateKey, string organization, CancellationToken cancellationToken)
    {
        var jwt = CreateAppJwt(appId, privateKey);

        using var client = httpClientFactory.CreateClient(GitHubApiDefaults.HttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        // Which installation of this App owns the course's organization.
        using var installationResponse = await client.GetAsync(
            new Uri($"orgs/{Uri.EscapeDataString(organization)}/installation", UriKind.Relative), cancellationToken);
        await EnsureSuccessAsync(installationResponse, $"looking up the App installation on '{organization}'", cancellationToken);

        using var installationDocument = await ReadJsonAsync(installationResponse, cancellationToken);
        var installationId = installationDocument.RootElement.GetProperty("id").GetInt64();

        // Exchange the App JWT for a token scoped to that installation.
        using var tokenResponse = await client.PostAsync(
            new Uri($"app/installations/{installationId}/access_tokens", UriKind.Relative), content: null, cancellationToken);
        await EnsureSuccessAsync(tokenResponse, "creating an installation access token", cancellationToken);

        using var tokenDocument = await ReadJsonAsync(tokenResponse, cancellationToken);
        var root = tokenDocument.RootElement;

        var permissions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (root.TryGetProperty("permissions", out var permissionsElement) && permissionsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var permission in permissionsElement.EnumerateObject())
                permissions[permission.Name] = permission.Value.GetString() ?? string.Empty;
        }

        var repositorySelection = root.TryGetProperty("repository_selection", out var selection) ? selection.GetString() : null;

        return new GitHubInstallationToken(
            root.GetProperty("token").GetString() ?? string.Empty,
            installationId,
            permissions,
            repositorySelection);
    }

    /// <summary>
    /// The App-level JWT: RS256, ten minutes, issued by the App id. GitHub accepts nothing else at
    /// <c>/app/*</c>, and it is only ever used to obtain the installation token.
    /// </summary>
    private static string CreateAppJwt(string appId, string privateKey)
    {
        var now = DateTimeOffset.UtcNow;

        // 60 seconds of backdating absorbs clock skew between this server and GitHub, which otherwise rejects
        // the JWT outright ("'iat' is in the future").
        var header = """{"alg":"RS256","typ":"JWT"}""";
        var payload = $$"""{"iat":{{now.AddSeconds(-60).ToUnixTimeSeconds()}},"exp":{{now.AddMinutes(10).ToUnixTimeSeconds()}},"iss":"{{appId}}"}""";

        var signingInput = $"{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(header))}.{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payload))}";

        using var rsa = ImportPrivateKey(privateKey);
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64Url.EncodeToString(signature)}";
    }

    /// <summary>
    /// Accepts either the PEM file GitHub hands out on key creation, or the bare base64 DER body that
    /// github-monitor's <c>AHK_GitHubAppPrivateKey</c> holds — an administrator migrating a course should be
    /// able to paste what they already have.
    /// </summary>
    private static RSA ImportPrivateKey(string privateKey)
    {
        var value = privateKey.Trim();
        var rsa = RSA.Create();

        try
        {
            if (value.Contains("-----BEGIN", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(value);
                return rsa;
            }

            var der = Convert.FromBase64String(new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()));
            try
            {
                rsa.ImportRSAPrivateKey(der, out _);
            }
            catch (CryptographicException)
            {
                rsa.ImportPkcs8PrivateKey(der, out _);
            }

            return rsa;
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var message = await GitHubApiDefaults.ReadErrorMessageAsync(response, cancellationToken);
        throw new GitHubOperationException(operation, response.StatusCode, message);
    }
}
