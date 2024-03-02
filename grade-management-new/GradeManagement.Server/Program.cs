
using GradeManagement.Data.Data;
using GradeManagement.Services.Services;
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
            builder.Services.AddTransient<AssignmentEventService>();
            builder.Services.AddTransient<CourseService>();
            builder.Services.AddTransient<GroupService>();
            builder.Services.AddTransient<LanguageService>();
            builder.Services.AddTransient<PullRequestService>();
            builder.Services.AddTransient<ScoreService>();
            builder.Services.AddTransient<SemesterService>();
            builder.Services.AddTransient<StudentService>();
            builder.Services.AddTransient<SubjectService>();
            builder.Services.AddTransient<TaskService>();
            builder.Services.AddTransient<TeacherService>();

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
