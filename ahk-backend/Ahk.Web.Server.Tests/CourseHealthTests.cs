using Ahk.Web.Data;
using Ahk.Web.Data.Entities;
using Ahk.Web.Services.Health;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Covers the course health checks and how their results aggregate. The network-bound GitHub check is not
/// exercised here; the local checks and the aggregation rule are what the admin dashboard's traffic lights
/// are actually derived from.
/// </summary>
public class CourseHealthTests
{
    private sealed class NoCourseProvider : ICurrentCourseProvider
    {
        public int? CurrentCourseId => null;
    }

    private static ApplicationDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(dbName).Options, new NoCourseProvider());

    private static Course CourseWith(CourseGitHubConfig? config, string? org = "ahk-viaubc01") =>
        new() { Id = 1, Slug = "viaubc01", Name = "Sample", GitHubOrganization = org, GitHubConfig = config };

    // ---- Webhook settings ----

    [Fact]
    public async Task WebhookCheck_ReportsNotConfigured_WithoutAnOrganization()
    {
        var result = await new WebhookConfigurationHealthCheck().RunAsync(CourseWith(new CourseGitHubConfig(), org: null));
        Assert.Equal(HealthStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task WebhookCheck_WarnsWhenTheSignatureCannotBeValidated()
    {
        var result = await new WebhookConfigurationHealthCheck().RunAsync(CourseWith(new CourseGitHubConfig()));
        Assert.Equal(HealthStatus.Warning, result.Status);
        Assert.NotNull(result.Remediation);
    }

    [Fact]
    public async Task WebhookCheck_WarnsWhenTheIntegrationIsSwitchedOff()
    {
        var config = new CourseGitHubConfig { GitHubWebhookSecret = "s3cret", Enabled = false };
        var result = await new WebhookConfigurationHealthCheck().RunAsync(CourseWith(config));
        Assert.Equal(HealthStatus.Warning, result.Status);
    }

    [Fact]
    public async Task WebhookCheck_PassesWhenOrganizationAndSecretAreSet()
    {
        var config = new CourseGitHubConfig { GitHubWebhookSecret = "s3cret", Enabled = true };
        var result = await new WebhookConfigurationHealthCheck().RunAsync(CourseWith(config));
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    // ---- CI callback token ----

    [Fact]
    public async Task TokenCheck_ReportsNotConfigured_WhenNoTokenWasEverIssued()
    {
        await using var db = CreateContext(Guid.NewGuid().ToString());
        var result = await new CiCallbackTokenHealthCheck(db).RunAsync(CourseWith(new CourseGitHubConfig()));
        Assert.Equal(HealthStatus.NotConfigured, result.Status);
    }

    [Fact]
    public async Task TokenCheck_FailsWhenEveryTokenIsRevoked()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(dbName))
        {
            seed.CourseWebhookTokens.Add(new CourseWebhookToken
            {
                CourseId = 1,
                Token = "t",
                Secret = "s",
                RevokedAt = DateTimeOffset.UtcNow,
            });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var result = await new CiCallbackTokenHealthCheck(db).RunAsync(CourseWith(new CourseGitHubConfig()));
        Assert.Equal(HealthStatus.Failed, result.Status);
    }

    /// <summary>
    /// The check runs in the host/admin context, where no current course is set — so it must bypass the course
    /// query filter, or it would report every course as having no token.
    /// </summary>
    [Fact]
    public async Task TokenCheck_SeesTokens_EvenWithNoCurrentCourseResolved()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(dbName))
        {
            seed.CourseWebhookTokens.Add(new CourseWebhookToken { CourseId = 1, Token = "t", Secret = "s" });
            await seed.SaveChangesAsync();
        }

        await using var db = CreateContext(dbName);
        var result = await new CiCallbackTokenHealthCheck(db).RunAsync(CourseWith(new CourseGitHubConfig()));
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    // ---- Aggregation ----

    [Theory]
    [InlineData(HealthStatus.Healthy, HealthStatus.NotConfigured, HealthStatus.Healthy)]
    [InlineData(HealthStatus.Healthy, HealthStatus.Warning, HealthStatus.Warning)]
    [InlineData(HealthStatus.Warning, HealthStatus.Failed, HealthStatus.Failed)]
    [InlineData(HealthStatus.NotConfigured, HealthStatus.NotConfigured, HealthStatus.NotConfigured)]
    public void Report_TakesTheWorstCheckStatus(HealthStatus first, HealthStatus second, HealthStatus expected)
    {
        var report = new CourseHealthReport
        {
            Checks = new[]
            {
                new HealthCheckResult { Status = first },
                new HealthCheckResult { Status = second },
            },
        };

        Assert.Equal(expected, report.Status);
    }

    [Fact]
    public void Report_WithNoChecks_IsNotConfigured() =>
        Assert.Equal(HealthStatus.NotConfigured, new CourseHealthReport().Status);
}
