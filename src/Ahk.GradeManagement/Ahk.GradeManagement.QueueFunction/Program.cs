using Ahk.GradeManagement.Bll;
using Ahk.GradeManagement.Bll.Mapping;
using Ahk.GradeManagement.Dal;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddBllServices();
        services.AddGradeManagementDbContext(context.Configuration, "DbConnection");
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
    })
    .Build();

host.Run();
