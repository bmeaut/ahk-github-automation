using AzureKeyVaultEmulator.Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var keyVault = builder.AddAzureKeyVaultEmulator("KeyVault", new() { Persist = true });

var dbConnection = builder.AddConnectionString("DbConnection");

var queueStorage = builder.AddAzureStorage("ahk-storage")
    .RunAsEmulator()
    .AddQueues("ahk-queue-storage");

builder.AddProject<Projects.Ahk_GradeManagement_Api>("ahk-grademanagement-api")
    .WithReference(keyVault)
    .WithReference(dbConnection);

builder.AddAzureFunctionsProject<Projects.Ahk_GradeManagement_QueueFunction>("ahk-grademanagement-queuefunction")
    .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(keyVault)
    .WithReference(queueStorage)
    .WithReference(dbConnection);

builder.Build().Run();
