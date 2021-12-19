using System.IO;
using System.Reflection;

namespace Ahk.GitHub.Monitor.Tests
{
    internal static class SampleData
    {
        public static readonly SampleCallbackData BranchCreate = new SampleCallbackData(getTextFromResource("branch_create.json"), "sha256=42197ad6cad74be8674363735038df64ca2ae8e8bdf027da60164adfbc561f4b", "create");
        public static string CommentDelete = getTextFromResource("comment_delete.json");
        public static string CommentEdit = getTextFromResource("comment_edit.json");
        public static string CommentCommand = getTextFromResource("comment_command.json");
        public static string PrOpen = getTextFromResource("pr_open.json");
        public static string PrReviewRequested = getTextFromResource("pr_reviewrequested.json");
        public static string PrReviewComment = getTextFromResource("pr_reviewcomment.json");
        public static string WorkflowRun = getTextFromResource("workflow_run.json");

        private static string getTextFromResource(string resourceName)
        {
            using var s = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Ahk.GitHub.Monitor.Tests.SampleCallbacks.{resourceName}");
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
    }
}
