using System.IO;
using System.Reflection;

namespace Ahk.GitHub.Monitor.Tests
{
    internal static class SampleData
    {
        public static readonly SampleCallbackData BranchCreate = new SampleCallbackData(getTextFromResource("branch_create.json"), "sha1=a3e5f87fb18d2aa2d54ae39510d1a8f1830eab28", "create");
        public static string CommentDelete = getTextFromResource("comment_delete.json");
        public static string CommentEdit = getTextFromResource("comment_edit.json");
        public static string PrOpen = getTextFromResource("pr_open.json");
        public static string PrReviewRequested = getTextFromResource("pr_reviewrequested.json");

        private static string getTextFromResource(string resourceName)
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Ahk.GitHub.Monitor.Tests.SampleCallbacks.{resourceName}");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
