using Ahk.GitHub.Monitor.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ahk.GitHub.Monitor.Tests.UnitTests;

[TestClass]
public class GitHubSignatureValidatorTest
{
    private const string Secret = "Wcks02cnncc67c33";

    [DataTestMethod]
    [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n",
        "sha256=3926a12bd47c5e3fe91cb2e6dd0c605438ac469c4de09e560b97029a3f751a88")]
    [DataRow("qqqq\r\nsdfsdfsdfsdf\r\nwwwwwwwwwwwww\r\n",
        "sha256=0d5a916d47e3a2d6ebaa1ca9fafb425e122f892edb8464496a2c8107169ba828")]
    [DataRow("aaaaaaqqqqqqqqqqqqqqq", "sha256=9abd46d0b161c9b171c36c6e2b88fd27d498ee08555cb4f34d39ddb2467273fe")]
    public void SignatureIsValid(string payload, string expectedSignature) =>
        Assert.IsTrue(GitHubSignatureValidator.IsSignatureValid(payload, expectedSignature, Secret));

    [TestMethod]
    public void SignatureIsValidActualJson()
    {
        var payload = SampleData.BranchCreate.Body;
        var expectedSignature = SampleData.BranchCreate.Signature;
        var secret = IntegrationTests.FunctionBuilder.AppConfig.GitHubWebhookSecret;
        Assert.IsTrue(GitHubSignatureValidator.IsSignatureValid(payload, expectedSignature, secret));
    }

    [DataTestMethod]
    [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=dummy")]
    [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "sha1=aaaaaaaa")]
    [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "dummy")]
    [DataRow("aaaaaa\r\nbbbbbbb\r\ncccccccccc\r\n", "")]
    public void SignatureIsNotValid(string payload, string expectedSignature) =>
        Assert.IsFalse(GitHubSignatureValidator.IsSignatureValid(payload, expectedSignature, Secret));
}
