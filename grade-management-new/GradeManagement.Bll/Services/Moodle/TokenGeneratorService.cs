using Jose;

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;

namespace GradeManagement.Bll.Services.Moodle;

public class TokenGeneratorService
{
    private const string Scope =
        "https://purl.imsglobal.org/spec/lti-ags/scope/lineitem https://purl.imsglobal.org/spec/lti-ags/scope/lineitem.readonly https://purl.imsglobal.org/spec/lti-ags/scope/result.readonly https://purl.imsglobal.org/spec/lti-ags/scope/score";

    public string? GenerateAccessToken()
    {
        var token = MakeClientAssertionToken();

        string endPoint = "https://edu.vik.bme.hu/mod/lti/token.php";
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

    private static string MakeClientAssertionToken()
    {
        var beginRSA = "-----BEGIN RSA PRIVATE KEY-----";
        var endRSA = "-----END RSA PRIVATE KEY-----";

        string privateKey =
            "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEAtJaqgj1WQABa25TOGjHZ8eIuBDH58C5f6YTaLFyvianOJJNe\ncrvXIEh4R6Egc7jFx2Ev5CNWIEoD/ft+XzMTQGVjxsZHhY0jUrON8ZY+1zlupdJD\n6GlqgT7SIzq7KAAH/3+L/VeijUraDEhoKhcKjHrY00wt0vr9PP6/pJD+IsyuH6ox\nQ2cq434puKriDo+7PckvW43W83gniQyKmF+nccWoHLL8kKDFU1wajwyS4AuRclLc\nLRp7IPO/EMpLwRDLElEuo9SSdiib8wbyUpJ8wymRvzS2Ek7IM64cqIfBW64kpLI3\n6IYCev+wZdkIMV/TtkkLkHgYWQho+LR4GYSH1wIDAQABAoIBAESVvCg+l1UzTU57\n01LLgBSHbCaXvG7ljfOoSxvyD5De9rWZapN0l51hKJ07qpGIqUSxuniQmxMkSSPa\nsgIB6dvZJe3GPD+SfMnZ+5y3DSK8YzODCdtovdRcQX6zvYTRjjgQ/t+2uruio/Fq\nRnVFzvHPbi3Bjt3ECQ1zSuF7V6lrIx3anQQ/F/97Wbi4yRPLlZYFD35jsC80dzCr\ngsPAEsqe7R2E1Ex8CB2nX7gsWvARfYUSpNR+WZBb+9FL1jM6pr1ts5AAzGhCVQUR\nglbFnYFfnhH+Ne87Tpyj5AuqOfQMB85OC7SAq1SG7QLa/SRjQpm3AilIL6fikgZS\n/63FRQECgYEA6GZwiB6ypoE/l5Y9buqUq5Lh8N7TOyxvYsNLCL23gq0dFIHuFuAm\nbuMhp/wWY4EiDCymWOnFlg7e6G61a06stAPczjNMnTtgXInOiBJMI2jT7kD9UZpH\nc2wvBD4kljKtb6QletidxujelNBiQX8GvNWXcfGy+4wabF78BDHSbsECgYEAxu1Q\nAwxZOQHrXw6iLI6hyqMUcUgOaCgpWSgOHh2F3hnyXZ969/vNvrFmo7N7nVDWajIR\niRflJ7t79OESbxzpaGl5fROmyfV+Pg89USGv2jJFroCUP5y79mG6no95JE6wJ4kJ\nYAlh6LNT1uFuTRcZJWP1TITY+VYSJo4btJdmNJcCgYB3MsOZLZWYDUbeqzKLR0pF\nziqQ7tkMyre+wkgkDZqoLb5ynEnP9dwAmALVNFkPZFZgRC52AEFVu/7c3Ju0lD/E\nfQ6tvGYZZzD/hbcm16uxpby9wRus1SK8sspStMTzPL70Og73OU+DjEFNtqwOx+Ze\nyHbK/Js+phePahB83kj+gQKBgQClT4UUY2iqBTxSLFj86jLtsIRGd3jxeZ6S5sSE\npkgfADT3NJb+CZU7CTWgiIELhKgKbD1CTkcys40ZPQkSqVYWXMCG/pO2dXpJnGR6\nTaJTkz9w+o04J8nTv/dT2Sr0zyd4U1slIebpeE0N3xzIl49gESdCRYwB+FLxE1tW\nwhI/VQKBgFW6mqzEUezG3m4ZJ+VpEpcbLLMRU+2aQpjwHBf5eYoddnM7tKwzcu2G\n5ZbgOBgOdFJAt2u6wIPBu9vjD3sNyKmaZotoitGYPmgIoa3ubBPXCRxfc+8slMCu\naM4CPmX471q1U5j1Vra23mNu1zmQ1J7v6gBdDOrDaLcCW0dReESB\n-----END RSA PRIVATE KEY-----";

        privateKey = privateKey.Trim();
        privateKey = privateKey.Remove(0, beginRSA.Length);
        privateKey = privateKey.Remove(privateKey.IndexOf(endRSA), endRSA.Length);
        privateKey = beginRSA + privateKey + endRSA;

        var claims = new List<Claim>();
        claims.Add(new Claim("iss", "app.mm-home.eu"));
        claims.Add(new Claim("iat", ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));
        claims.Add(new Claim("exp",
            ((int)(DateTime.UtcNow.AddHours(1) - new DateTime(1970, 1, 1)).TotalSeconds).ToString()));
        claims.Add(new Claim("aud", "https://edu.vik.bme.hu/mod/lti/token.php"));
        claims.Add(new Claim("sub", "eYoyDIMQtrZ02xV"));

        return CreateToken(claims, privateKey);
    }

    private static string CreateToken(List<Claim> claims, string privateRsaKey)
    {
        RSAParameters rsaParams;
        using (var tr = new StringReader(privateRsaKey))
        {
            var pemReader = new PemReader(tr);
            var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
            if (keyPair == null)
            {
                throw new Exception("Could not read RSA private key");
            }

            var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
            rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
        }

        using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
        {
            rsa.ImportParameters(rsaParams);
            Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
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
