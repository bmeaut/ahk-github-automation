using AutSoft.Common.Exceptions;

using GradeManagement.Data.Data;
using GradeManagement.Bll;
using GradeManagement.Server.ExceptionHandlers;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Logging;

namespace GradeManagement.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                    {
                        options.Authority = "https://login.microsoftonline.com/6a3548ab-7570-4271-91a8-58da00697029/";
                        options.Audience = "fed1f289-3643-41e3-8a22-cc46db1547d9";
                    }, options => { builder.Configuration.Bind("AzureAd", options); },
                    subscribeToJwtBearerMiddlewareDiagnosticsEvents: true);

            builder.Services.AddDbContext<GradeManagementDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Add services to the container.

            builder.Services.AddControllersWithViews();
            builder.Services.AddOpenApiDocument();
            builder.Services.AddRazorPages();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddAutoMapper(typeof(Program).Assembly);

            builder.Services.AddTransient<AssignmentService>();
            builder.Services.AddTransient<AssignmentLogService>();
            builder.Services.AddTransient<AssingmentEventConsumerService>();
            builder.Services.AddTransient<CourseService>();
            builder.Services.AddTransient<GroupService>();
            builder.Services.AddTransient<LanguageService>();
            builder.Services.AddTransient<PullRequestService>();
            builder.Services.AddTransient<ScoreService>();
            builder.Services.AddTransient<SemesterService>();
            builder.Services.AddTransient<StudentService>();
            builder.Services.AddTransient<SubjectService>();
            builder.Services.AddTransient<ExerciseService>();
            builder.Services.AddTransient<UserService>();
            builder.Services.AddTransient<GroupTeacherService>();
            builder.Services.AddTransient<SubjectTeacherService>();

            builder.Services.AddExceptionHandler<EntityNotFoundExceptionHandler>();
            builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
            builder.Services.AddExceptionHandler<DefaultExceptionHandler>();
            builder.Services.AddProblemDetails();

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

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
            app.Run();
            IdentityModelEventSource.ShowPII = true;
            IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        }
    }
}
