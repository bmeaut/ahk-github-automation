using System;
using System.Collections.Generic;

namespace Ahk.GitHub.Monitor
{
    public class WebhookResult
    {
        public List<string> Messages = new List<string>();

        public void LogInfo(string message) => Messages.Add(message);
        public void LogError(Exception ex, string message) => Messages.Add(message + ": " + ex.Message);
    }
}
