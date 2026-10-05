using System.Net;
using Ahk.Web.Services.GitHub;
using Octokit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

public class GitHubRepositoryServiceTests
{
    [Fact]
    public void BillingError_ProducesAnActionableMessage()
    {
        var exception = new ApiException(GitHubErrorFormatterTests.Response(
            HttpStatusCode.UnprocessableEntity,
            body: """{"message":"Validation Failed","errors":[{"resource":"OrganizationMember","field":"user","code":"billing_error"}]}"""));

        var message = GitHubRepositoryService.TranslateApiError(exception);

        Assert.Contains("seats", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnrecognisedError_KeepsGitHubsOwnMessage()
    {
        var exception = new ApiException(GitHubErrorFormatterTests.Response(
            HttpStatusCode.UnprocessableEntity,
            body: """{"message":"Validation Failed","errors":[{"resource":"Repository","field":"name","code":"invalid"}]}"""));

        var message = GitHubRepositoryService.TranslateApiError(exception);

        Assert.Equal("Validation Failed", message);
    }
}
