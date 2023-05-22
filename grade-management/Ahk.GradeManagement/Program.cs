using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.Grademanagement.Data.Internal;
using Ahk.GradeManagement.Data;
using Ahk.GradeManagement.Data.Entities;
using Ahk.GradeManagement.Functions.Assignments;
using Ahk.GradeManagement.Functions.Groups;
using Ahk.GradeManagement.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Ahk.GradeManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddMemoryCache(setup =>
                    {
                        setup.ExpirationScanFrequency = TimeSpan.FromMinutes(4);
                    });
                    services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
                    services.AddScoped<ResultProcessing.IResultProcessor, ResultProcessing.ResultProcessor>();
                    services.AddScoped<Services.ITokenManagementService, Services.TokenManagementService>();
                    services.AddScoped<SetGrade.ISetGradeService, SetGrade.SetGradeService>();
                    services.AddScoped<ListGrades.IGradeListing, ListGrades.GradeListing>();
                    services.AddScoped<StatusTracking.IStatusTrackingService, StatusTracking.StatusTrackingService>();
                    services.AddScoped<IGroupService, GroupService>();
                    services.AddScoped<IAssignmentService, AssignmentService>();
                    services.AddScoped<IStatusTrackingRepository, StatusTrackingRepository>();

                    string azureSqlConnString = Environment.GetEnvironmentVariable("SQLAZURECONNSTR_AHK_ConnString");

                    services.AddAhkData();
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
            }

            await host.RunAsync();
        }
    }
}
