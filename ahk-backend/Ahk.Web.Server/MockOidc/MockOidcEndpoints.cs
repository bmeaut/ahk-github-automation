using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ahk.Web.Server.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Ahk.Web.Server.MockOidc;

/// <summary>
/// Minimal in-app OpenID Provider for development only. Exists because just the production redirect URI
/// (https://ahk.aut.bme.hu/signin-oidc) is registered with the BME IdP, so the real provider cannot be used
/// from localhost.
///
/// It mirrors the shape of BME's discovery document (query response mode, client_secret_post, the same claim
/// names including neptun_code) and issues genuinely signed RS256 id_tokens, so the ASP.NET OIDC handler runs
/// its real validation path against it.
/// </summary>
public static class MockOidcEndpoints
{
    public const string BasePath = "/mock-oidc";

    /// <summary>Holds the developer's chosen persona between /persona and the next /authorize.</summary>
    private const string PersonaCookie = "ahk_mock_oidc_persona";

    public static IEndpointRouteBuilder MapMockOidcProvider(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(BasePath).AllowAnonymous();

        group.MapGet("/.well-known/openid-configuration", (IOptions<OidcOptions> options) =>
        {
            var issuer = Issuer(options.Value);
            return Results.Json(new
            {
                issuer,
                authorization_endpoint = $"{issuer}/authorize",
                token_endpoint = $"{issuer}/token",
                userinfo_endpoint = $"{issuer}/userinfo",
                jwks_uri = $"{issuer}/keyset",
                response_types_supported = new[] { "code" },
                subject_types_supported = new[] { "public" },
                grant_types_supported = new[] { "authorization_code" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
                scopes_supported = new[] { "openid", "profile", "email", "userinfo", "offline_access" },
                response_modes_supported = new[] { "query", "fragment", "form_post" },
                claims_supported = new[]
                {
                    "aud", "iss", "sub", "iat", "exp", "auth_time", "email", "name",
                    "family_name", "given_name", "eduperson_scoped_affiliation", "neptun_code",
                },
            });
        });

        // Persona selection. The OIDC handler builds the authorize URL itself, so a custom parameter cannot be
        // threaded through the challenge — the choice is parked in a cookie instead.
        // Usage: navigate to /mock-oidc/persona?user=student, then sign in as usual.
        group.MapGet("/persona", (HttpContext context, string? user) =>
        {
            var selected = MockOidcUsers.Resolve(user);
            context.Response.Cookies.Append(PersonaCookie, selected.Key, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
            });

            return Results.Json(new { persona = selected.Key, available = MockOidcUsers.All.Keys });
        });

        // No login UI: pick the persona (query, else cookie, else default) and redirect straight back with a code.
        group.MapGet("/authorize", (HttpContext context, MockOidcCodeStore codes, IOptions<OidcOptions> options) =>
        {
            var query = context.Request.Query;
            var redirectUri = query["redirect_uri"].ToString();
            var clientId = query["client_id"].ToString();

            if (string.IsNullOrEmpty(redirectUri))
                return Results.BadRequest(new { error = "invalid_request", error_description = "redirect_uri is required" });

            if (!string.Equals(clientId, options.Value.ClientId, StringComparison.Ordinal))
                return Results.BadRequest(new { error = "unauthorized_client", error_description = $"unexpected client_id '{clientId}'" });

            var personaKey = query["mock_user"].ToString() is { Length: > 0 } q
                ? q
                : context.Request.Cookies[PersonaCookie];
            var user = MockOidcUsers.Resolve(personaKey);
            var code = codes.Issue(user, query["nonce"].ToString() is { Length: > 0 } n ? n : null, clientId);

            var separator = redirectUri.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            var location = $"{redirectUri}{separator}code={Uri.EscapeDataString(code)}";

            var state = query["state"].ToString();
            if (!string.IsNullOrEmpty(state))
                location += $"&state={Uri.EscapeDataString(state)}";

            return Results.Redirect(location);
        });

        group.MapPost("/token", async (HttpContext context, MockOidcCodeStore codes, MockOidcSigningKey signingKey, IOptions<OidcOptions> options) =>
        {
            var form = await context.Request.ReadFormAsync();
            var redeemed = codes.Redeem(form["code"].ToString());
            if (redeemed is null)
                return Results.BadRequest(new { error = "invalid_grant", error_description = "unknown or already-used code" });

            // client_secret_post: the handler sends credentials in the body.
            if (!string.Equals(form["client_secret"].ToString(), options.Value.ClientSecret, StringComparison.Ordinal))
                return Results.BadRequest(new { error = "invalid_client", error_description = "client_secret mismatch" });

            var issuer = Issuer(options.Value);
            var now = DateTimeOffset.UtcNow;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, redeemed.User.Subject),
                new(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new("auth_time", now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            };

            if (redeemed.Nonce is not null)
                claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, redeemed.Nonce));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: redeemed.ClientId,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: now.AddMinutes(10).UtcDateTime,
                signingCredentials: signingKey.SigningCredentials);

            var idToken = new JwtSecurityTokenHandler().WriteToken(token);

            // The access token is opaque here; the userinfo endpoint resolves it through the code store's persona.
            return Results.Json(new
            {
                access_token = $"mock-access-{redeemed.User.Key}",
                token_type = "Bearer",
                expires_in = 600,
                id_token = idToken,
            });
        });

        group.MapGet("/userinfo", (HttpContext context) =>
        {
            var authorization = context.Request.Headers.Authorization.ToString();
            const string prefix = "Bearer mock-access-";
            if (!authorization.StartsWith(prefix, StringComparison.Ordinal))
                return Results.Unauthorized();

            var user = MockOidcUsers.Resolve(authorization[prefix.Length..]);

            // Claim names deliberately match BME's claims_supported.
            var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["sub"] = user.Subject,
                ["email"] = user.Email,
                ["name"] = user.Name,
                ["given_name"] = user.GivenName,
                ["family_name"] = user.FamilyName,
            };

            if (user.NeptunCode is not null)
                payload["neptun_code"] = user.NeptunCode;

            if (user.Affiliations.Length > 0)
                payload["eduperson_scoped_affiliation"] = user.Affiliations; // multi-valued, as at BME

            return Results.Json(payload);
        });

        group.MapGet("/keyset", (MockOidcSigningKey signingKey)
            => Results.Json(new { keys = new[] { signingKey.ToJsonWebKey() } }));

        return endpoints;
    }

    /// <summary>
    /// The mock's issuer is the configured Authority, so the handler's issuer validation matches without any
    /// special-casing (dev config points Authority at this app's own /mock-oidc).
    /// </summary>
    private static string Issuer(OidcOptions options) => options.Authority!.TrimEnd('/');
}
