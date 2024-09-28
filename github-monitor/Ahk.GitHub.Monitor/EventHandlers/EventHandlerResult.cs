namespace Ahk.GitHub.Monitor.EventHandlers
{
    public class EventHandlerResult(string result)
    {
        public string Result { get; } = result;

        public static EventHandlerResult PayloadError(string message) => new EventHandlerResult($"payload error: {message}");
        public static EventHandlerResult NoActionNeeded(string message) => new EventHandlerResult($"no action needed: {message}");
        public static EventHandlerResult ActionPerformed(string message) => new EventHandlerResult($"action performed: {message}");
        public static EventHandlerResult EventNotOfInterest(string action) => new EventHandlerResult($"action not of interest: {action}");
        public static EventHandlerResult Disabled(string message = null) => new EventHandlerResult(message == null ? $"event handler disabled" : $"event handler disabled: {message}");
    }
}
