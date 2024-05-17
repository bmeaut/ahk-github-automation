using GradeManagement.Bll;

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
        services.AddTransient<AssignmentService>();
        services.AddTransient<AssignmentLogService>();
        services.AddTransient<AssignmentEventProcessorService>();
        services.AddTransient<CourseService>();
        services.AddTransient<GroupService>();
        services.AddTransient<LanguageService>();
        services.AddTransient<PullRequestService>();
        services.AddTransient<ScoreService>();
        services.AddTransient<SemesterService>();
        services.AddTransient<StudentService>();
        services.AddTransient<SubjectService>();
        services.AddTransient<ExerciseService>();
        services.AddTransient<UserService>();
        services.AddTransient<SubjectTeacherService>();
        services.AddDbContext<GradeManagement.Data.Data.GradeManagementDbContext>(options =>
        {
            var connectionString = context.Configuration.GetConnectionString("DbConnection");
            options.UseSqlServer(connectionString);
        });
        services.AddAutoMapper(typeof(GradeManagement.Server.Program).Assembly);
    })
    .Build();

host.Run();
