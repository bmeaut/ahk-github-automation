using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ahk.GitHub.Monitor.EventHandlers.Parsers;

internal static class ConfigYamlParser
{
    public static bool IsEnabled(string fileContent)
    {
        if (string.IsNullOrEmpty(fileContent))
            return false;

        return new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build()
            .Deserialize<AhkConfig>(fileContent)
            .Enabled;
    }

    internal class AhkConfig
    {
        public bool Enabled { get; set; }
    }
}
