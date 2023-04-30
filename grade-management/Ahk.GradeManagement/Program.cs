using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ahk.GradeManagement.Data;
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

                    string azureSqlConnString = Environment.GetEnvironmentVariable("AhkContext");

                    services.AddAhkData();
                    services.AddDbContext<AhkDbContext>(options => options.UseSqlServer(azureSqlConnString));
                })
                .Build();
            await host.RunAsync();
        }
    }
}
