using YamlDotNet.Serialization;

namespace Ahk.GitHub.Monitor.EventHandlers.Parsers;

internal static class ConfigYamlParser
{
    public static bool IsEnabled(string fileContent)
    {
        if (string.IsNullOrEmpty(fileContent))
            return false;

        return new Deserializer().Deserialize<AhkConfig>(fileContent).Enabled;
    }

    internal class AhkConfig
    {
        public bool Enabled { get; set; }
    }
}
