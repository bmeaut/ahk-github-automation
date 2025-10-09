using System;

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using AzureKeyVaultEmulator.Aspire.Client;

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Ahk.GitHub.Monitor.Config;

public static class ConfigurationBuilderExtensions
{
    public static FunctionsApplicationBuilder AddAzureKeyVaultConfiguration(this FunctionsApplicationBuilder builder)
    {
        var kvConnString = builder.Configuration.GetConnectionString("KeyVault");
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddAzureKeyVaultEmulator(kvConnString);
            builder.Configuration.AddAzureKeyVault(
                new SecretClient(new Uri(kvConnString), new EmulatedTokenCredential(kvConnString), new()
                {
                    DisableChallengeResourceVerification = true
                }),
                new AzureKeyVaultConfigurationOptions());
        }
        else
        {
            builder.Services.AddAzureClients(client =>
            {
                client.AddSecretClient(new Uri(kvConnString));
            });

            builder.Configuration.AddAzureKeyVault(new Uri(kvConnString), new DefaultAzureCredential());
        }

        return builder;
    }
}
