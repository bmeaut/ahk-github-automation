using System.Security.Claims;
using Ahk.Web.Data.Entities;

namespace Ahk.Web.Server.Auth;

/// <summary>
/// Projects an external (BME OIDC) identity onto the local user record. Applied on every login, not just at
/// creation, so a user's directory data stays current.
/// </summary>
public static class ExternalClaimsMapper
{
    /// <summary>Copies the mapped claims onto <paramref name="user"/>. Returns true when anything changed.</summary>
    public static bool SyncFromClaims(ApplicationUser user, ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(principal);

        var changed = false;

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email) && !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            user.Email = email;
            changed = true;
        }

        var displayName = ResolveDisplayName(principal);
        if (!string.IsNullOrWhiteSpace(displayName) && !string.Equals(user.DisplayName, displayName, StringComparison.Ordinal))
        {
            user.DisplayName = displayName;
            changed = true;
        }

        // Absent claims must not wipe existing values — a sparse directory account should not clear a known code.
        var neptun = principal.FindFirstValue(BmeClaimTypes.NeptunCode);
        if (!string.IsNullOrWhiteSpace(neptun) && !string.Equals(user.NeptunCode, neptun, StringComparison.OrdinalIgnoreCase))
        {
            user.NeptunCode = neptun.ToUpperInvariant();
            changed = true;
        }

        var affiliation = ResolveAffiliation(principal);
        if (!string.IsNullOrWhiteSpace(affiliation) && !string.Equals(user.Affiliation, affiliation, StringComparison.Ordinal))
        {
            user.Affiliation = affiliation;
            changed = true;
        }

        return changed;
    }

    /// <summary>Prefers the provider's <c>name</c>, otherwise composes it from given/family name.</summary>
    public static string? ResolveDisplayName(ClaimsPrincipal principal)
    {
        var name = principal.FindFirstValue(ClaimTypes.Name) ?? principal.FindFirstValue("name");
        if (!string.IsNullOrWhiteSpace(name))
            return name;

        var given = principal.FindFirstValue(ClaimTypes.GivenName) ?? principal.FindFirstValue("given_name");
        var family = principal.FindFirstValue(ClaimTypes.Surname) ?? principal.FindFirstValue("family_name");
        var composed = string.Join(' ', new[] { given, family }.Where(s => !string.IsNullOrWhiteSpace(s)));

        return string.IsNullOrWhiteSpace(composed) ? null : composed;
    }

    /// <summary>
    /// eduperson_scoped_affiliation is multi-valued. The claim action joins array values, but a provider may
    /// also emit repeated claims — handle both and normalize to a single ';'-separated string.
    /// </summary>
    public static string? ResolveAffiliation(ClaimsPrincipal principal)
    {
        var values = principal.FindAll(BmeClaimTypes.Affiliation)
            .SelectMany(c => c.Value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return values.Length == 0 ? null : string.Join(';', values);
    }
}
