using System;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace Ahk.GitHub.Monitor.Config;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddAzureKeyVaultIfConfigured(this IConfigurationBuilder builder)
    {
        var keyVaultUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");

        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            var credential = new DefaultAzureCredential();
            builder.AddAzureKeyVault(new Uri(keyVaultUri), credential);
        }

        return builder;
    }
}
