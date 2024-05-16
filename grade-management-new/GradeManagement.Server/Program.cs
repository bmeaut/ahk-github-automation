
using AutSoft.Common.Exceptions;
using GradeManagement.Data.Data;
using GradeManagement.Bll;
using GradeManagement.Server.ExceptionHandlers;
using Microsoft.EntityFrameworkCore;

namespace GradeManagement.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
            builder.Services.AddTransient<AssignmentEventProcessorService>();
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

            app.UseAuthorization();

            app.MapRazorPages();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
