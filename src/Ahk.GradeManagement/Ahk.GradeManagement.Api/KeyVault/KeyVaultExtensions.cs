using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using AzureKeyVaultEmulator.Aspire.Client;

using Microsoft.Extensions.Azure;

namespace Ahk.GradeManagement.Api.KeyVault;

public static class KeyVaultExtensions
{
    public static WebApplicationBuilder AddKeyVault(this WebApplicationBuilder builder)
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
