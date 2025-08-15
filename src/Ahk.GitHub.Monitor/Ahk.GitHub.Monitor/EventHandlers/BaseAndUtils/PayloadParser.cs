using System;
using Microsoft.Extensions.Logging;
using Octokit;
using Octokit.Internal;

namespace Ahk.GitHub.Monitor.EventHandlers.BaseAndUtils;

public class PayloadParser<TPayload>
    where TPayload : ActivityPayload
{
    public static bool TryParsePayload(string requestBody, out TPayload payload, out EventHandlerResult errorResult, ILogger logger)
    {
        payload = null;
        if (string.IsNullOrEmpty(requestBody))
        {
            errorResult = EventHandlerResult.PayloadError("request body was empty");
            logger.LogError("request body was empty");
            return false;
        }

        try
        {
            payload = new SimpleJsonSerializer().Deserialize<TPayload>(requestBody);
        }
        catch (Exception ex)
        {
            errorResult = EventHandlerResult.PayloadError($"request body deserialization failed: {ex.Message}");
            logger.LogError(ex, "request body deserialization failed");
            return false;
        }

        if (payload == null)
        {
            errorResult = EventHandlerResult.PayloadError("parsed payload was null or empty");
            logger.LogError("parsed payload was null or empty");
            return false;
        }

        if (payload.Repository == null)
        {
            errorResult = EventHandlerResult.PayloadError("no repository information in webhook payload");
            logger.LogError("no repository information in webhook payload");
            return false;
        }

        errorResult = null;
        return true;
    }
}
