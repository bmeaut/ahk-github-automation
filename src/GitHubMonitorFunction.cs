using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ahk.GitHub.Monitor
{
    public static partial class GitHubMonitorFunction
    {
        [FunctionName("github-webhook")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
            ILogger logger)
        {
            string statusMessage = "n/a";
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN", EnvironmentVariableTarget.Process);
            if (string.IsNullOrWhiteSpace(githubToken))
            {
                logger.LogError("GITHUB_TOKEN settings not provided");
                statusMessage = "err: GITHUB_TOKEN settings not provided";
            }
            else
            {
                string eventName = request.Headers.GetValueOrDefault("X-GitHub-Event");
                string deliveryId = request.Headers.GetValueOrDefault("X-GitHub-Delivery");
                // string signature = request.Headers.GetValueOrDefault("X-Hub-Signature");

                logger.LogInformation("Webhook delivery: Delivery id = '{DeliveryId}', Event name = '{EventName}'", deliveryId, eventName);

                string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
                try
                {
                    switch (eventName)
                    {
                        case "create":
                            statusMessage = await handleBranchCreated(requestBody, githubToken, logger);
                            break;
                        case "issue_comment":
                            statusMessage = await handleIssueComment(requestBody, githubToken, logger);
                            break;
                        default:
                            logger.LogInformation("Unknown event {EventType}", eventName);
                            statusMessage = "inf: event not of interrest: " + eventName;
                            break;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to handle webhook");
                    statusMessage = "err: exception: " + ex.Message;
                }
            }

            return new OkObjectResult(new { message = statusMessage });
        }

        private static GitHubClient getGitHubClient(string githubToken)
        {
            var github = new GitHubClient(new ProductHeaderValue("Ahk"), new InMemoryCredentialStore(new Credentials(githubToken)));
            github.SetRequestTimeout(TimeSpan.FromSeconds(5));
            return github;
        }

        private static bool isRepositoryOfInterrest(Repository repository)
        {
            var repositoryPrefixes = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY_PREFIXES", EnvironmentVariableTarget.Process);
            if (string.IsNullOrEmpty(repositoryPrefixes))
                return true;
            return repositoryPrefixes.Split(';').Any(prefix => repository.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<string> handleBranchCreated(string requestBody, string githubToken, ILogger logger)
        {
            var webhookPayload = new SimpleJsonSerializer().Deserialize<CreateEventPayload>(requestBody);
            if (webhookPayload == null)
            {
                logger.LogError("Cannot parse payload");
                return "err: cannot parse payload";
            }
            else if (webhookPayload.Repository == null)
            {
                logger.LogError("Did not receive repository information in webhook payload");
                return "err: no repository information in payload";
            }
            else if (!isRepositoryOfInterrest(webhookPayload.Repository))
            {
                logger.LogInformation("Repository {Repository} is not of interrest", webhookPayload.Repository.FullName);
                return "inf: repository is none of interrest";
            }
            else if (webhookPayload.RefType.StringValue.Equals("branch", StringComparison.OrdinalIgnoreCase) && webhookPayload.Ref.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Triggered for repository {Repository}", webhookPayload.Repository.FullName);
                await getGitHubClient(githubToken).Repository.Branch.UpdateBranchProtection(webhookPayload.Repository.Id, webhookPayload.Ref,
                    new BranchProtectionSettingsUpdate(
                        requiredStatusChecks: null, // Required. Require status checks to pass before merging. Set to null to disable.
                        requiredPullRequestReviews: new BranchProtectionRequiredReviewsUpdate(false, false, 1),
                        // restrictions: new BranchProtectionPushRestrictionsUpdate(),
                        enforceAdmins: false)); // Required. Enforce all configured restrictions for administrators. Set to true to enforce required status checks for repository administrators. Set to null to disable.
                return "ok: handled";
            }
            else
            {
                logger.LogInformation("Branch {Branch} is not of interrest", webhookPayload.Ref);
                return $"inf: branch {webhookPayload.Ref} not of interrest";
            }
        }

        private static async Task<string> handleIssueComment(string requestBody, string githubToken, ILogger logger)
        {
            var webhookPayload = new SimpleJsonSerializer().Deserialize<IssueEventPayload>(requestBody);
            if (webhookPayload == null)
            {
                logger.LogError("Cannot parse payload");
                return "err: cannot parse payload";
            }
            else if (webhookPayload.Repository == null)
            {
                logger.LogError("Did not receive repository information in webhook payload");
                return "err: no repository information in payload";
            }
            else if (webhookPayload.Issue == null)
            {
                logger.LogError("Did not receive issue information in webhook payload");
                return "err: no issue information in payload";
            }
            else if (!isRepositoryOfInterrest(webhookPayload.Repository))
            {
                logger.LogInformation("Repository {Repository} is not of interrest", webhookPayload.Repository.FullName);
                return "inf: repository is none of interrest";
            }
            else if (webhookPayload.Action.Equals("edited", StringComparison.OrdinalIgnoreCase) || webhookPayload.Action.Equals("deleted", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Triggered for repository {Repository} # {IssueId}", webhookPayload.Repository.FullName, webhookPayload.Issue.Number);
                await getGitHubClient(githubToken).Issue.Comment.Create(webhookPayload.Repository.Id, webhookPayload.Issue.Number, "@akosdudas issue comment change");
                return "ok: handled";
            }
            else
            {
                logger.LogInformation("Issue action {Action} is not of interrest", webhookPayload.Action);
                return $"inf: action {webhookPayload.Action} not of interrest";
            }
        }
    }
}
