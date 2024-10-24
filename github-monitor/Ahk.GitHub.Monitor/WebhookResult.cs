using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor
{
    public class WebhookResult
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "Result object is JSON serialized.")]
        public List<string> Messages = [];

        public void LogInfo(string message) => Messages.Add(message);
        public void LogError(Exception ex, string message) => Messages.Add(message + ": " + ex.ToString());
    }
}
