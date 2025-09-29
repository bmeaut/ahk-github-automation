using Ahk.GradeManagement.Api.Authorization;
using Ahk.GradeManagement.Api.ErrorHandling;
using Ahk.GradeManagement.Api.KeyVault;
using Ahk.GradeManagement.Api.RequestContext;
using Ahk.GradeManagement.Backend.Common.Options;
using Ahk.GradeManagement.Bll;
using Ahk.GradeManagement.Dal;

using Microsoft.Identity.Web;

using MudBlazor.Services;

using MudExtensions.Services;

namespace Ahk.GradeManagement.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.ConfigureOption<AhkOptions>(builder.Configuration);

        builder.AddKeyVault();

        builder.Services.AddRequestContext();
        builder.Services.AddHttpClient();

        builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

        builder.Services.AddClaimsTransformation();
        builder.Services.AddPolicies();
        builder.Services.AddRequirementHandlers();

        builder.Services.AddGradeManagementDbContext(builder.Configuration, "DbConnection");

        builder.Services.AddControllersWithViews();
        builder.Services.AddOpenApiDocument(config =>
        {
            config.DocumentName = "AHK2.OpenAPI";
            config.Title = "AHK Grade Management API";
        });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddBllServices();

        builder.Services.AddCustomProblemDetails(builder.Configuration);

        builder.Services.AddRazorPages();
        builder.Services.AddMudServices();
        builder.Services.AddMudExtensions();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.UseOpenApi();
            app.UseSwaggerUi();
            app.UseWebAssemblyDebugging();
        }

        app.UseHttpsRedirection();

        app.UseExceptionHandler();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
