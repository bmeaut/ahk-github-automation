using Ahk.Web.Services.GitHubWebhooks;
using Ahk.Web.Services.Integrations;
using Xunit;

namespace Ahk.Web.Server.Tests.GitHubWebhooks;

/// <summary>
/// GitHub's <c>X-Hub-Signature-256</c> scheme. Vectors ported verbatim from
/// <c>github-monitor/.../UnitTests/GitHubSignatureValidatorTest.cs</c>: reproducing them is what proves the
/// portal accepts exactly the deliveries the Azure Function did.
/// </summary>
public class GitHubSignatureValidatorTests
{
    private const string Secret = "Wcks02cnncc67c33";

    [Theory]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha256=3926a12bd47c5e3fe91cb2e6dd0c605438ac469c4de09e560b97029a3f751a88")]
    [InlineData("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "sha256=0d5a916d47e3a2d6ebaa1ca9fafb425e122f892edb8464496a2c8107169ba828")]
    [InlineData("aaaaaaqqqqqqqqqqqqqqq", "sha256=9abd46d0b161c9b171c36c6e2b88fd27d498ee08555cb4f34d39ddb2467273fe")]
    public void SignatureIsValid(string payload, string expectedSignature)
        => Assert.True(GitHubSignatureValidator.IsSignatureValid(payload, expectedSignature, Secret));

    [Theory]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=dummy")]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=aaaaaaaa")]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "dummy")]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "")]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", null)]
    public void SignatureIsNotValid(string payload, string? receivedSignature)
        => Assert.False(GitHubSignatureValidator.IsSignatureValid(payload, receivedSignature, Secret));

    /// <summary>A course with no secret stored must never accidentally validate.</summary>
    [Fact]
    public void MissingSecretNeverValidates()
        => Assert.False(GitHubSignatureValidator.IsSignatureValid(
            "aaaaaa", "sha256=3926a12bd47c5e3fe91cb2e6dd0c605438ac469c4de09e560b97029a3f751a88", secret: null));
}

/// <summary>
/// The CI callback's HMAC scheme.
///
/// These three vectors exist identically in <em>both</em>
/// <c>grade-management/Ahk.GradeManagement.Tests/UnitTests/HmacSha256ValidatorTest.cs</c> and the Go client's
/// <c>publish-results-pr/internal/publishtoapi/hmacsignature_test.go</c>. Reproducing them here is the proof
/// that the portal is wire-compatible with the evaluator container already running in student repositories —
/// which cannot be redeployed on our schedule.
/// </summary>
public class HmacSha256ValidatorTests
{
    private const string Secret = "Wcks02cnncc67c33";
    private const string HttpVerb = "POST";
    private const string HttpUrl = "https://my.url.com/address";

    private static readonly DateTime Date = new(2021, 9, 1, 13, 34, 56, DateTimeKind.Utc);

    [Theory]
    [InlineData("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "SGAhL9hfzLqi30G1uqtQyErRC4oKBlxT9NImaJ/V9CQ=")]
    [InlineData("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n", "K7lZXguubpUONKhHh40lAzxt2vPyZnm6LkjLhrYPwAo=")]
    [InlineData("aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=")]
    public void SignatureIsValid(string payload, string expectedSignature)
        => Assert.True(HmacSha256Validator.IsSignatureValid(HttpVerb, HttpUrl, Date, payload, expectedSignature, Secret));

    [Fact]
    public void SignatureIsNotValidWhenVerbDiffers()
        => Assert.False(HmacSha256Validator.IsSignatureValid(
            "PUT", HttpUrl, Date, "aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=", Secret));

    [Fact]
    public void SignatureIsNotValidWhenUrlDiffers()
        => Assert.False(HmacSha256Validator.IsSignatureValid(
            HttpVerb, "https://my.url.com/other", Date, "aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=", Secret));

    [Fact]
    public void SignatureIsNotValidWhenDateDiffers()
        => Assert.False(HmacSha256Validator.IsSignatureValid(
            HttpVerb, HttpUrl, Date.AddSeconds(1), "aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=", Secret));

    [Fact]
    public void SignatureIsNotValidWhenPayloadDiffers()
        => Assert.False(HmacSha256Validator.IsSignatureValid(
            HttpVerb, HttpUrl, Date, "aaaaaaqqqqqqqqqqqqqqqa", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=", Secret));

    /// <summary>
    /// Both sides lower-case the URL before signing, so a differently cased <c>AHK_APPURL</c> still verifies.
    /// The only forgiving part of an otherwise byte-exact contract, and worth knowing when diagnosing one.
    /// </summary>
    [Fact]
    public void UrlCasingDoesNotMatter()
        => Assert.True(HmacSha256Validator.IsSignatureValid(
            HttpVerb, "https://MY.URL.com/Address", Date, "aaaaaaqqqqqqqqqqqqqqq", "cN9KEIb9uO7VskC9mmZ7wWkzqOXirFXcjqB3i4cK0mA=", Secret));

    /// <summary>Four parts, single LF separators, no trailing newline.</summary>
    [Fact]
    public void StringToSignHasTheDocumentedShape()
    {
        var stringToSign = HmacSha256Validator.GetStringToSign("post", "HTTPS://My.Url.Com/Address", Date, "body");

        Assert.Equal("POST\nhttps://my.url.com/address\nWed, 01 Sep 2021 13:34:56 GMT\nbody", stringToSign);
    }
}
