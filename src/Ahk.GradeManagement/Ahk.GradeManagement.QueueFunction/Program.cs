using Ahk.GradeManagement.Backend.Common.RequestContext;
using Ahk.GradeManagement.Bll;
using Ahk.GradeManagement.Bll.Mapping;
using Ahk.GradeManagement.Dal;
using Ahk.GradeManagement.QueueFunction.RequestContext;

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using AzureKeyVaultEmulator.Aspire.Client;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

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
builder.Services.AddBllServices();
builder.Services.AddSingleton<IRequestContext, QueueFunctionRequestContext>();
builder.Services.AddGradeManagementDbContext(builder.Configuration, "DbConnection");
builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

builder.Build().Run();
