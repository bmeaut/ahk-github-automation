using GradeManagement.Bll;
using GradeManagement.Bll.Profiles;
using GradeManagement.Bll.Services;
using GradeManagement.Data;
using GradeManagement.Data.Data;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
