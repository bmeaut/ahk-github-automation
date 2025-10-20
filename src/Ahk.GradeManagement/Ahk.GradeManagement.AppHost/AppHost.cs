using AzureKeyVaultEmulator.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVaultEmulator("KeyVault", new() { Persist = true });

var dbConnection = builder.AddConnectionString("DbConnection");

var serviceBus = builder.AddAzureServiceBus("ahk-servicebus")
    .RunAsEmulator()
    .AddServiceBusQueue("ahk-events");

var webhook = builder.AddAzureFunctionsProject<Projects.Ahk_GitHub_Monitor>("ahk-github-monitor")
    .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(keyVault)
    .WithReference(serviceBus);

var webhookTunnel = builder.AddDevTunnel("github-webhook-tunnel")
    .WithReference(webhook, allowAnonymous: true);

var backendTunnel = builder.AddDevTunnel("github-post-install-tunnel");

var backend = builder.AddProject<Projects.Ahk_GradeManagement_Api>("ahk-grademanagement-api")
    .WithReference(keyVault)
    .WithReference(dbConnection)
    .WithReference(serviceBus)
    .WithReference(webhook, webhookTunnel);

backendTunnel.WithReference(backend, allowAnonymous: true);

backend.WithReference(backend, backendTunnel);

builder.Build().Run();
