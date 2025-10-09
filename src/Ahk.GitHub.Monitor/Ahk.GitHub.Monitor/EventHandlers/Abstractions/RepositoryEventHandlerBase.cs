using Ahk.GitHub.Monitor.EventHandlers.Parsers;
using Ahk.GitHub.Monitor.Services.GitHubClientFactory;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Octokit;

using System;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor.EventHandlers.Abstractions;

public abstract class RepositoryEventHandlerBase<TPayload>(IGitHubClientFactory gitHubClientFactory, IMemoryCache cache, ILogger logger) : IGitHubEventHandler
    where TPayload : ActivityPayload
{
    static string IGitHubEventHandler.GitHubWebhookEventName => throw new NotImplementedException("Cannot make abstract static interface members abstract yet");

    protected ILogger Logger { get; } = logger;

    protected IGitHubClient GitHubClient { get; private set; }

    public async Task<EventHandlerResult> ExecuteAsync(string requestBody)
    {
        if (!PayloadParser.TryParsePayload(requestBody, out TPayload webhookPayload, out var errorResult, Logger))
            return errorResult;

        GitHubClient = await gitHubClientFactory.CreateGitHubClientAsync(webhookPayload.Repository.Owner.Login, webhookPayload.Installation.Id, Logger);

        if (!await IsEnabledForRepositoryAsync(webhookPayload))
        {
            Logger.LogInformation("no ahk-monitor.yml or disabled");
            return EventHandlerResult.Disabled("no ahk-monitor.yml or disabled");
        }

        return await ExecuteCoreAsync(webhookPayload);
    }

    protected abstract Task<EventHandlerResult> ExecuteCoreAsync(TPayload webhookPayload);

    protected Task<bool> GetIsUserOrganizationMemberAsync(TPayload webhookPayload, string username)
    {
        if (webhookPayload.Repository.Owner.Type != AccountType.Organization)
            return Task.FromResult(false);

        return cache.GetOrCreateAsync(
            $"githubisorgmember{webhookPayload.Repository.Owner.Login}{username}",
            async cacheEntry =>
            {
                var isMember = await GetIsUserOrganizationMemberAsync(webhookPayload.Repository.Owner.Login, username);
                cacheEntry.SetValue(isMember);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(1));
                return isMember;
            });
    }

    private async Task<bool> GetIsUserOrganizationMemberAsync(string org, string user)
    {
        try
        {
            return await GitHubClient.Organization.Member.CheckMember(org, user);
        }
        catch (NotFoundException)
        {
            return false;
        }
    }

    private Task<bool> IsEnabledForRepositoryAsync(TPayload webhookPayload)
        => cache.GetOrCreateAsync(
            $"ahkmonitorisenabledinrepo{webhookPayload.Repository.Id}",
            async cacheEntry =>
            {
                var isEnabled = await GetConfigIsEnabledInRepositoryAsync(webhookPayload);
                cacheEntry.SetValue(isEnabled);
                cacheEntry.SetAbsoluteExpiration(TimeSpan.FromHours(12));
                return isEnabled;
            });

    private async Task<bool> GetConfigIsEnabledInRepositoryAsync(TPayload webhookPayload)
    {
        try
        {
            var contents = await GitHubClient.Repository.Content.GetAllContentsByRef(
                webhookPayload.Repository.Id,
                ".github/ahk-monitor.yml",
                webhookPayload.Repository.DefaultBranch);
            if (contents.Count == 0)
                return false;

            var contentAsString = contents[0].Content;
            return ConfigYamlParser.IsEnabled(contentAsString);
        }
        catch (NotFoundException)
        {
            return false;
        }
    }
}
