using System.Collections.Concurrent;

namespace Ahk.Web.Server.MockOidc;

/// <summary>A fixed persona the mock provider can authenticate as, mirroring BME's claim set.</summary>
public sealed record MockOidcUser(
    string Key,
    string Subject,
    string Email,
    string Name,
    string GivenName,
    string FamilyName,
    string? NeptunCode,
    string[] Affiliations);

/// <summary>
/// Fixed development personas. The default is an instructor; <c>?mock_user=student</c> (or <c>=noclaims</c>)
/// selects another so claim handling — multi-valued affiliation, missing neptun code — can be exercised.
/// </summary>
public static class MockOidcUsers
{
    public const string DefaultKey = "instructor";

    public static readonly IReadOnlyDictionary<string, MockOidcUser> All = new Dictionary<string, MockOidcUser>(StringComparer.OrdinalIgnoreCase)
    {
        [DefaultKey] = new(
            Key: DefaultKey,
            Subject: "mock-sub-instructor",
            Email: "teacher@bme.hu",
            Name: "Teszt Tanár",
            GivenName: "Teszt",
            FamilyName: "Tanár",
            NeptunCode: "TEACH1",
            Affiliations: new[] { "staff@bme.hu", "employee@bme.hu" }),

        ["student"] = new(
            Key: "student",
            Subject: "mock-sub-student",
            Email: "student@bme.hu",
            Name: "Teszt Hallgató",
            GivenName: "Teszt",
            FamilyName: "Hallgató",
            NeptunCode: "ABC123",
            Affiliations: new[] { "student@bme.hu" }),

        // Directory account with no neptun code and no affiliation — the sparse-claims case.
        ["noclaims"] = new(
            Key: "noclaims",
            Subject: "mock-sub-noclaims",
            Email: "sparse@bme.hu",
            Name: "Sparse User",
            GivenName: "Sparse",
            FamilyName: "User",
            NeptunCode: null,
            Affiliations: Array.Empty<string>()),
    };

    public static MockOidcUser Resolve(string? key)
        => key is not null && All.TryGetValue(key, out var user) ? user : All[DefaultKey];
}

/// <summary>Authorization codes issued by the mock, held in memory until redeemed at the token endpoint.</summary>
public sealed class MockOidcCodeStore
{
    private readonly ConcurrentDictionary<string, MockOidcAuthorizationCode> codes = new(StringComparer.Ordinal);

    public string Issue(MockOidcUser user, string? nonce, string clientId)
    {
        var code = Guid.NewGuid().ToString("N");
        codes[code] = new MockOidcAuthorizationCode(user, nonce, clientId);
        return code;
    }

    /// <summary>Single-use, like a real authorization code.</summary>
    public MockOidcAuthorizationCode? Redeem(string? code)
        => code is not null && codes.TryRemove(code, out var value) ? value : null;
}

public sealed record MockOidcAuthorizationCode(MockOidcUser User, string? Nonce, string ClientId);
