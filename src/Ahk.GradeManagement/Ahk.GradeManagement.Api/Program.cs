using Ahk.GradeManagement.Api.Authorization;
using Ahk.GradeManagement.Api.Middlewares;
using Ahk.GradeManagement.Api.Middlewares.ExceptionHandlers;
using Ahk.GradeManagement.Bll;
using Ahk.GradeManagement.Bll.Profiles;
using Ahk.GradeManagement.Dal;

using Azure.Identity;

using Microsoft.Identity.Web;

using MudBlazor.Services;

using MudExtensions.Services;

namespace Ahk.GradeManagement.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var keyVaultUri = builder.Configuration["KEY_VAULT_URI"];
        if (!string.IsNullOrEmpty(keyVaultUri))
        {
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUri),
                new DefaultAzureCredential());
        }

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddHttpClient();

        builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);

        builder.Services.AddClaimsTransformation();
        builder.Services.AddPolicies();
        builder.Services.AddRequirementHandlers();

        builder.Services.AddGradeManagementDbContext(builder.Configuration, "DbConnection");

        // Add services to the container.

        builder.Services.AddControllersWithViews();
        builder.Services.AddOpenApiDocument(config =>
        {
            config.Title = "AHK Grade Management API";
        });
        builder.Services.AddRazorPages();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

        builder.Services.AddBllServices();

        builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddMudServices();
        builder.Services.AddMudExtensions();

        var app = builder.Build();


        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // Add OpenAPI 3.0 document serving middleware
            // Available at: https://localhost:7136/swagger/v1/swagger.json
            app.UseOpenApi();
            // Add web UIs to interact with the document
            // Available at: https://localhost:7136/swagger
            app.UseSwaggerUi();
            app.UseWebAssemblyDebugging();
        }

        app.UseHttpsRedirection();

        app.UseExceptionHandler();
        app.UseMiddleware<HeaderMiddleware>();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapRazorPages();
        app.MapControllers();
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
