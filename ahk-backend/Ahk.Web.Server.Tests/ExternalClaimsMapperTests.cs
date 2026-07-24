using System.Security.Claims;
using Ahk.Web.Data.Entities;
using Ahk.Web.Server.Auth;
using Xunit;

namespace Ahk.Web.Server.Tests;

/// <summary>
/// Covers projecting a BME OIDC identity onto the local user: the claim names, the multi-valued affiliation,
/// and the rule that absent claims must never wipe values already stored.
/// </summary>
public class ExternalClaimsMapperTests
{
    private static ClaimsPrincipal Principal(params Claim[] claims)
        => new(new ClaimsIdentity(claims, authenticationType: "oidc"));

    [Fact]
    public void SyncFromClaims_MapsAllBmeClaims()
    {
        var user = new ApplicationUser();
        var principal = Principal(
            new Claim(ClaimTypes.Email, "teacher@bme.hu"),
            new Claim(ClaimTypes.Name, "Teszt Tanár"),
            new Claim(BmeClaimTypes.NeptunCode, "teach1"),
            new Claim(BmeClaimTypes.Affiliation, "staff@bme.hu;employee@bme.hu"));

        var changed = ExternalClaimsMapper.SyncFromClaims(user, principal);

        Assert.True(changed);
        Assert.Equal("teacher@bme.hu", user.Email);
        Assert.Equal("Teszt Tanár", user.DisplayName);
        Assert.Equal("TEACH1", user.NeptunCode); // normalized to upper, like the domain model
        Assert.Equal("staff@bme.hu;employee@bme.hu", user.Affiliation);
    }

    [Fact]
    public void SyncFromClaims_ComposesDisplayNameFromGivenAndFamilyName()
    {
        var user = new ApplicationUser();
        var principal = Principal(
            new Claim(ClaimTypes.GivenName, "Teszt"),
            new Claim(ClaimTypes.Surname, "Tanár"));

        ExternalClaimsMapper.SyncFromClaims(user, principal);

        Assert.Equal("Teszt Tanár", user.DisplayName);
    }

    [Fact]
    public void SyncFromClaims_JoinsRepeatedAffiliationClaims()
    {
        var principal = Principal(
            new Claim(BmeClaimTypes.Affiliation, "staff@bme.hu"),
            new Claim(BmeClaimTypes.Affiliation, "employee@bme.hu"),
            new Claim(BmeClaimTypes.Affiliation, "staff@bme.hu")); // duplicate collapses

        Assert.Equal("staff@bme.hu;employee@bme.hu", ExternalClaimsMapper.ResolveAffiliation(principal));
    }

    [Fact]
    public void SyncFromClaims_SparseIdentity_LeavesOptionalFieldsNull()
    {
        var user = new ApplicationUser();
        var principal = Principal(new Claim(ClaimTypes.Email, "sparse@bme.hu"));

        ExternalClaimsMapper.SyncFromClaims(user, principal);

        Assert.Equal("sparse@bme.hu", user.Email);
        Assert.Null(user.NeptunCode);
        Assert.Null(user.Affiliation);
    }

    [Fact]
    public void SyncFromClaims_MissingClaims_DoNotWipeStoredValues()
    {
        var user = new ApplicationUser
        {
            Email = "teacher@bme.hu",
            DisplayName = "Teszt Tanár",
            NeptunCode = "TEACH1",
            Affiliation = "staff@bme.hu",
        };

        // A later login where the directory returned nothing extra.
        var changed = ExternalClaimsMapper.SyncFromClaims(user, Principal());

        Assert.False(changed);
        Assert.Equal("TEACH1", user.NeptunCode);
        Assert.Equal("staff@bme.hu", user.Affiliation);
    }

    [Fact]
    public void SyncFromClaims_ReportsNoChange_WhenValuesAlreadyMatch()
    {
        var user = new ApplicationUser { Email = "teacher@bme.hu", DisplayName = "Teszt Tanár", NeptunCode = "TEACH1" };
        var principal = Principal(
            new Claim(ClaimTypes.Email, "teacher@bme.hu"),
            new Claim(ClaimTypes.Name, "Teszt Tanár"),
            new Claim(BmeClaimTypes.NeptunCode, "TEACH1"));

        Assert.False(ExternalClaimsMapper.SyncFromClaims(user, principal));
    }

    [Fact]
    public void SyncFromClaims_UpdatesChangedDirectoryData()
    {
        var user = new ApplicationUser { Email = "old@bme.hu", DisplayName = "Old Name", NeptunCode = "OLD123" };
        var principal = Principal(
            new Claim(ClaimTypes.Email, "new@bme.hu"),
            new Claim(ClaimTypes.Name, "New Name"),
            new Claim(BmeClaimTypes.NeptunCode, "NEW456"));

        Assert.True(ExternalClaimsMapper.SyncFromClaims(user, principal));
        Assert.Equal("new@bme.hu", user.Email);
        Assert.Equal("New Name", user.DisplayName);
        Assert.Equal("NEW456", user.NeptunCode);
    }
}
