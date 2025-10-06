using AzureKeyVaultEmulator.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVaultEmulator("KeyVault", new() { Persist = true });

var dbConnection = builder.AddConnectionString("DbConnection");

var queueStorage = builder.AddAzureStorage("ahk-storage")
    .RunAsEmulator()
    .AddQueues("ahk-queue-storage");

var webhook = builder.AddAzureFunctionsProject<Projects.Ahk_GitHub_Monitor>("ahk-github-monitor")
    .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(keyVault)
    .WithReference(queueStorage);

var webhookTunnel = builder.AddDevTunnel("github-webhook-tunnel")
    .WithReference(webhook, allowAnonymous: true);

var queueFunction = builder.AddAzureFunctionsProject<Projects.Ahk_GradeManagement_QueueFunction>("ahk-grademanagement-queuefunction")
    .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(keyVault)
    .WithReference(queueStorage)
    .WithReference(dbConnection);

var backendTunnel = builder.AddDevTunnel("github-post-install-tunnel");

var backend = builder.AddProject<Projects.Ahk_GradeManagement_Api>("ahk-grademanagement-api")
    .WithReference(keyVault)
    .WithReference(dbConnection)
    .WithReference(webhook, webhookTunnel);

backendTunnel.WithReference(backend, allowAnonymous: true);

backend.WithReference(backend, backendTunnel);

builder.Build().Run();
