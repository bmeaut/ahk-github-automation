namespace Ahk.GradeManagement.Tests.IntegrationTests
{
    public class SampleCallbackData
    {
        public SampleCallbackData(string token, string secret, string signature, string body)
        {
            this.Token = token;
            this.Secret = secret;
            this.Signature = signature;
            this.Body = body;
        }

        public string Token { get; }
        public string Secret { get; }
        public string Signature { get; }
        public string Body { get; }

        public static readonly SampleCallbackData Sample1 = new SampleCallbackData(
            token: "Akjr3897sLJlkj23",
            secret: "ljf98ddksnf343c",
            signature: "lHsmlcoMpr8vclm8DEVj/Juek5ZIfZK4+ph22h2W8dQ=",
            body: "{\"gitHubRepoName\":\"org/name\",\"gitHubBranch\":\"branch\",\"gitHubCommitHash\":\"aa11cc33\",\"gitHubPullRequestNum\":789,\"neptunCode\":\"ABC123\",\"imageFiles\":[],\"result\":[{\"exerciseName\":\"ex1\",\"taskName\":\"t1\",\"points\":2,\"comment\":\"line1 abc\\nlin2 end\"},{\"taskName\":\"t1\",\"points\":5}]}");

        public static readonly SampleCallbackData InvalidPayload = new SampleCallbackData(
            token: "Akjr3897sLJlkj23",
            secret: "ljf98ddksnf343c",
            signature: "T7yowPR4Zxkr9Fzwc4jGD+CNxoGvfpRVrtaetvsq0L4=",
            body: "notjson");
    }
}
