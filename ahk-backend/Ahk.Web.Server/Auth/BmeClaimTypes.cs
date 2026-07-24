namespace Ahk.Web.Server.Auth;

/// <summary>
/// Claim names published by the BME IdP (see its <c>claims_supported</c>). Used both for the OIDC claim
/// mappings and when syncing an external identity onto the local user.
/// </summary>
public static class BmeClaimTypes
{
    /// <summary>Student/staff Neptun code — the key of the domain model.</summary>
    public const string NeptunCode = "neptun_code";

    /// <summary>Multi-valued, e.g. ["staff@bme.hu", "employee@bme.hu"].</summary>
    public const string Affiliation = "eduperson_scoped_affiliation";
}
