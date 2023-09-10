using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ahk.GradeManagement.Services.AssignmentService;
using Ahk.GradeManagement.Services.GroupService;
using Ahk.GradeManagement.Services.SetGradeService;
using Ahk.GradeManagement.Services.StatusTrackingService;
using Ahk.GradeManagement.Helpers;

namespace Ahk.GradeManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults(workerApplication =>
                {
                    workerApplication.UseAspNetCoreIntegration();
                })
                .ConfigureAspNetCoreIntegration()
                .ConfigureServices(services =>
                {
                    services.AddMemoryCache(setup =>
                    {
                        setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
                    });
                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddScoped<ResultProcessing.IResultProcessor, ResultProcessing.ResultProcessor>();
                    services.AddScoped<Services.ITokenManagementService, Services.TokenManagementService>();
                    services.AddScoped<ISetGradeService, SetGradeService>();
                    services.AddScoped<ListGrades.IGradeListing, ListGrades.GradeListing>();
                    services.AddScoped<IStatusTrackingService, StatusTrackingService>();
                    services.AddScoped<IGroupService, GroupService>();
                    services.AddScoped<IAssignmentService, AssignmentService>();
                    services.AddScoped<IGradeService, GradeService>();

                    var mapper = MapperConfig.InitializeAutomapper();

                    services.AddSingleton(mapper);

                    string azureSqlConnString = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_AHK_ConnString");

                    services.AddDbContext<AhkDbContext>(options => options.UseSqlServer(azureSqlConnString));
                })
                .Build();

            using (var context = new AhkDbContextFactory().CreateDbContext(args))
            {
                context.Database.EnsureCreated();
                var token = context.WebhookTokens.FirstOrDefault();
                if (token == null)
                {
                    var id = Guid.NewGuid().ToString("N");
                    var secret = Guid.NewGuid().ToString("N");
                    var webHookToken = new WebhookToken() { Id = id, Secret = secret };
                    context.WebhookTokens.Add(webHookToken);
                    context.SaveChanges();
                }

                SampleDataSeeder seeder = new SampleDataSeeder(context);
                //seeder.ClearData();
                seeder.SeedData();
            }

            await host.RunAsync();
        }
    }
}
