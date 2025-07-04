using Ahk.GradeManagement.Dal.Entities;
using Ahk.GradeManagement.Shared.Config;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Ahk.GradeManagement.Shared.Exceptions;

using Jose;

using Microsoft.Extensions.Configuration;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Ahk.GradeManagement.Bll.Services.Moodle;

public class TokenGeneratorService(IConfiguration configuration)
{
    private const string Scope =
        "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem https://purl.imsglobal.org/spec/lti-ags/scope/lineitem.readonly https://purl.imsglobal.org/spec/lti-ags/scope/result.readonly https://purl.imsglobal.org/spec/lti-ags/scope/score";

    public async Task<string?> GenerateAccessToken(Course course)
    {
        var token = await MakeClientAssertionToken(course);

        var endPoint = "https://edu.vik.bme.hu/mod/lti/token.php";
        var client = new HttpClient();

        var data = new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"), new KeyValuePair<string, string>(
                "client_assertion_type",
                "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", token),
            new KeyValuePair<string, string>("scope", Scope),
        };
        var result = client.PostAsync(endPoint, new FormUrlEncodedContent(data)).GetAwaiter().GetResult();
        var content = result.Content.ReadFromJsonAsync<PlatformTokenResponse>().Result;

        return content?.Access_token;
    }

    private async Task<string> MakeClientAssertionToken(Course course)
    {
        var appUrl = configuration["APP_URL"];
        if (string.IsNullOrEmpty(appUrl)) throw new MoodleSyncException("App url was null or empty!");

        var claims = new List<Claim>();
        claims.Add(new Claim("iss", appUrl));
        claims.Add(new Claim("iat", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));
        claims.Add(new Claim("exp",
            ((int)(DateTime.UtcNow.AddHours(1) - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));
        claims.Add(new Claim("aud", "https://edu.vik.bme.hu/mod/lti/token.php"));
        claims.Add(new Claim("sub", course.MoodleClientId));

        return await CreateToken(claims, course);
    }

    private async Task<string> CreateToken(List<Claim> claims, Course course)
    {
        var keyVaultUrl = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
        if (keyVaultUrl == null)
            throw new Exception("Please set environment variable KEY_VAULT_URI");

        var secretClient = new SecretClient(new Uri(keyVaultUrl), new DefaultAzureCredential());
        var moodlePrivateKey =
            await secretClient.GetSecretAsync($"{MoodleConfig.Name}--{course.MoodleClientId}--MoodlePrivateKey");

        var privateRsaKey = moodlePrivateKey.Value.Value;
        if (string.IsNullOrEmpty(privateRsaKey)) throw new MoodleSyncException("Private RSA was null or empty!");

        privateRsaKey = privateRsaKey.Trim();

        RSAParameters rsaParams;
        using (var tr = new StringReader(privateRsaKey))
        {
            var pemReader = new PemReader(tr);
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            if (keyPair == null)
                throw new MoodleSyncException("Could not read RSA private key");

            var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
            rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
        }

        using (var rsa = new RSACryptoServiceProvider())
        {
            rsa.ImportParameters(rsaParams);
            var payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
            return JWT.Encode(payload, rsa, JwsAlgorithm.RS256);
        }
    }

    private class PlatformTokenResponse
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public int Expires_in { get; set; }
        public string Scope { get; set; }
    }
}
