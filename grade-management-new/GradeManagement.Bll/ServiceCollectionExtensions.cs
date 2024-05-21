using GradeManagement.Bll.Services;

using Microsoft.Extensions.DependencyInjection;

namespace GradeManagement.Bll;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBllServices(this IServiceCollection services)
    {
        services.AddTransient<AssignmentService>();
        services.AddTransient<DashboardService>();
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

        return services;
    }
}
