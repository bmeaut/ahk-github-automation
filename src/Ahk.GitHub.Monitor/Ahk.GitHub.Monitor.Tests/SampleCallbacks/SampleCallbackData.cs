namespace Ahk.GitHub.Monitor.Tests;

internal class SampleCallbackData
{
    public SampleCallbackData(string body, string signature, string eventName)
    {
        this.Body = body;
        this.Signature = signature;
        this.EventName = eventName;
    }

    public string Body { get; }
    public string Signature { get; }
    public string EventName { get; }
}
