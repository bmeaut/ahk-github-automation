using System;

namespace Ahk.GitHub.Monitor
{
    public class IsFunction
    {
        public static bool Enabled(string functionName)
        {
            var envvar = Environment.GetEnvironmentVariable(functionName.ToUpperInvariant(), EnvironmentVariableTarget.Process);

            if (string.IsNullOrEmpty(envvar))
                return false;

            if (envvar.Equals("0", StringComparison.OrdinalIgnoreCase))
                return false;
            if (envvar.Equals("false", StringComparison.OrdinalIgnoreCase))
                return false;
            if (envvar.Equals("disable", StringComparison.OrdinalIgnoreCase))
                return false;
            if (envvar.Equals("disabled", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }
    }
}
