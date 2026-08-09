using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Ahk.Web.Server.MockOidc;

/// <summary>
/// In-memory RSA key used by the development mock provider to sign id_tokens, and published through its JWKS.
/// Generated once per process — restarting the app rotates it, which is fine for a dev-only provider.
///
/// The mock signs tokens for real (rather than stubbing validation out) so the ASP.NET OIDC handler runs its
/// genuine signature/issuer/audience/nonce checks; otherwise the mock would prove nothing about the real flow.
/// </summary>
public sealed class MockOidcSigningKey : IDisposable
{
    public const string KeyId = "ahk-mock-key";

    private readonly RSA rsa;

    public MockOidcSigningKey()
    {
        rsa = RSA.Create(2048);
        SecurityKey = new RsaSecurityKey(rsa) { KeyId = KeyId };
        SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.RsaSha256);
    }

    public RsaSecurityKey SecurityKey { get; }

    public SigningCredentials SigningCredentials { get; }

    /// <summary>Public key as a JWKS entry (RFC 7517), which is what the handler fetches from the keyset endpoint.</summary>
    public object ToJsonWebKey()
    {
        var parameters = rsa.ExportParameters(includePrivateParameters: false);
        return new
        {
            kty = "RSA",
            use = "sig",
            alg = "RS256",
            kid = KeyId,
            n = Base64UrlEncoder.Encode(parameters.Modulus),
            e = Base64UrlEncoder.Encode(parameters.Exponent),
        };
    }

    public void Dispose() => rsa.Dispose();
}
