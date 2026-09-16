using Ahk.Web.Services.GitHub;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;

namespace Ahk.Web.Services.GitHubWebhooks.Handlers;

/// <summary>
/// Base for every handler that acts on a repository. Port of
/// <c>github-monitor/.../EventHandlers/RepositoryEventBase.cs</c>, with the GitHub client and the delivery's
/// identity moved onto <see cref="GitHubWebhookContext"/> so the handler itself holds no state.
///
/// Two behaviours are inherited by everything that derives from this and must not be bypassed: payload
/// deserialization with its four distinct error messages, and the <c>.github/ahk-monitor.yml</c> opt-in gate.
/// </summary>
public abstract class RepositoryEventHandlerBase<TPayload> : IGitHubWebhookHandler
    where TPayload : ActivityPayload
{
    /// <summary>
    /// How long a repository's opt-in answer is remembered. Long, because it is asked on every delivery and the
    /// answer almost never changes — with the consequence that <em>enabling</em> a repository can take up to
    /// half a day to be noticed. Restarting the application is the only faster flush.
    /// </summary>
    private static readonly TimeSpan EnabledCacheDuration = TimeSpan.FromHours(12);

    private static readonly TimeSpan NeptunCacheDuration = TimeSpan.FromHours(12);

    private static readonly TimeSpan OrganizationMemberCacheDuration = TimeSpan.FromHours(1);

    protected RepositoryEventHandlerBase(IMemoryCache cache, ILogger logger)
    {
        this.Cache = cache;
        this.Logger = logger;
    }

    public abstract string GitHubEventName { get; }

    protected ILogger Logger { get; }

    protected IMemoryCache Cache { get; }

    public async Task<EventHandlerResult> ExecuteAsync(GitHubWebhookContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!TryParsePayload(context.RequestBody, out var payload, out var errorResult))
            return errorResult;

        if (!await IsEnabledForRepositoryAsync(context, payload, cancellationToken))
        {
            Logger.LogInformation("no ahk-monitor.yml or disabled");
            return EventHandlerResult.Disabled("no ahk-monitor.yml or disabled");
        }

        return await ExecuteCoreAsync(context, payload, cancellationToken);
    }

    protected abstract Task<EventHandlerResult> ExecuteCoreAsync(GitHubWebhookContext context, TPayload payload, CancellationToken cancellationToken);

    protected bool TryParsePayload(string requestBody, out TPayload payload, out EventHandlerResult errorResult)
    {
        payload = null!;

        if (string.IsNullOrEmpty(requestBody))
        {
            errorResult = EventHandlerResult.PayloadError("request body was empty");
            Logger.LogError("request body was empty");
            return false;
        }

        try
        {
            payload = new SimpleJsonSerializer().Deserialize<TPayload>(requestBody);
        }
#pragma warning disable CA1031 // Any deserialization failure is reported to the delivery log, never thrown.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            errorResult = EventHandlerResult.PayloadError($"request body deserialization failed: {ex.Message}");
            Logger.LogError(ex, "request body deserialization failed");
            return false;
        }

        if (payload is null)
        {
            errorResult = EventHandlerResult.PayloadError("parsed payload was null or empty");
            Logger.LogError("parsed payload was null or empty");
            return false;
        }

        if (payload.Repository is null)
        {
            errorResult = EventHandlerResult.PayloadError("no repository information in webhook payload");
            Logger.LogError("no repository information in webhook payload");
            return false;
        }

        errorResult = null!;
        return true;
    }

    /// <summary>
    /// The student's Neptun code, read from <c>neptun.txt</c> on the given branch. Null when the file is
    /// missing — a repository whose owner never filled it in still gets its events recorded, just without a
    /// student attached.
    /// </summary>
    protected Task<string?> GetNeptunAsync(GitHubWebhookContext context, long repositoryId, string branchName, CancellationToken cancellationToken = default)
        => Cache.GetOrCreateAsync(
            key: $"neptuntxtfile{repositoryId}{branchName}",
            factory: async cacheEntry =>
            {
                var value = await GetNeptunTxtFileContentAsync(context, repositoryId, branchName, cancellationToken);
                cacheEntry.SetValue(value);
                cacheEntry.SetAbsoluteExpiration(NeptunCacheDuration);
                return value;
            });

    /// <summary>
    /// Whether a login belongs to the repository's organization — the portal's definition of "is staff, not a
    /// student". Non-organization repositories always answer false.
    /// </summary>
    protected Task<bool> IsUserOrganizationMemberAsync(GitHubWebhookContext context, TPayload payload, string username, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payload);

        if (payload.Repository.Owner.Type != AccountType.Organization)
            return Task.FromResult(false);

        return IsOrganizationMemberAsync(context, payload.Repository.Owner.Login, username, cancellationToken);
    }

    protected Task<bool> IsOrganizationMemberAsync(GitHubWebhookContext context, string organization, string username, CancellationToken cancellationToken = default)
        => Cache.GetOrCreateAsync(
            key: $"githubisorgmember{organization}{username}",
            factory: async cacheEntry =>
            {
                var isMember = await CheckOrganizationMemberAsync(context, organization, username, cancellationToken);
                cacheEntry.SetValue(isMember);
                cacheEntry.SetAbsoluteExpiration(OrganizationMemberCacheDuration);
                return isMember;
            });

    /// <summary>
    /// The opt-in gate. A repository is acted on only when its default branch carries
    /// <c>.github/ahk-monitor.yml</c> with <c>enabled: true</c> — otherwise every event from it is ignored.
    /// Cached per repository id, which is globally unique, so no course qualifier is needed in the key.
    /// </summary>
    private Task<bool> IsEnabledForRepositoryAsync(GitHubWebhookContext context, TPayload payload, CancellationToken cancellationToken)
        => Cache.GetOrCreateAsync(
            key: $"ahkmonitorisenabledinrepo{payload.Repository.Id}",
            factory: async cacheEntry =>
            {
                var isEnabled = await GetConfigIsEnabledInRepositoryAsync(context, payload, cancellationToken);
                cacheEntry.SetValue(isEnabled);
                cacheEntry.SetAbsoluteExpiration(EnabledCacheDuration);
                return isEnabled;
            });

    /// <summary>
    /// ⚠️ Only <see cref="NotFoundException"/> may become <c>false</c> here. A transient failure must be left
    /// to throw: answering <c>false</c> would cache "this repository is disabled" for twelve hours and
    /// silently mute a repository that is enabled, which is far worse than one failed delivery.
    /// </summary>
    private async Task<bool> GetConfigIsEnabledInRepositoryAsync(GitHubWebhookContext context, TPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            var contents = await GitHubCallRetry.IdempotentAsync(
                "read .github/ahk-monitor.yml",
                () => context.GitHubClient.Repository.Content.GetAllContentsByRef(
                    payload.Repository.Id, ".github/ahk-monitor.yml", payload.Repository.DefaultBranch),
                Logger,
                cancellationToken);

            if (contents.Count == 0)
                return false;

            return ConfigYamlParser.IsEnabled(contents[0].Content);
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    private async Task<string?> GetNeptunTxtFileContentAsync(GitHubWebhookContext context, long repositoryId, string branchName, CancellationToken cancellationToken)
    {
        try
        {
            var contents = await GitHubCallRetry.IdempotentAsync(
                "read neptun.txt",
                () => context.GitHubClient.Repository.Content.GetAllContentsByRef(repositoryId, "neptun.txt", branchName),
                Logger,
                cancellationToken);

            if (contents.Count == 0)
                return null;

            return contents[0].Content?.Trim();
        }
        catch (NotFoundException)
        {
            return null;
        }
    }

    private async Task<bool> CheckOrganizationMemberAsync(GitHubWebhookContext context, string organization, string username, CancellationToken cancellationToken)
    {
        try
        {
            return await GitHubCallRetry.IdempotentAsync(
                "check organization membership",
                () => context.GitHubClient.Organization.Member.CheckMember(organization, username),
                Logger,
                cancellationToken);
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
