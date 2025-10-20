using Ahk.GradeManagement.Bll.Consumers;
using Ahk.GradeManagement.Bll.Mapping;
using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Bll.Services.Moodle;

using MassTransit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ahk.GradeManagement.Bll;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBllServices(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);

        services.AddTransient<AssignmentService>();
        services.AddTransient<DashboardService>();
        services.AddTransient<AssignmentLogService>();
        services.AddTransient<AssignmentEventProcessorService>();
        services.AddTransient<CourseService>();
        services.AddTransient<GroupService>();
        services.AddTransient<LanguageService>();
        services.AddTransient<PullRequestService>();
        services.AddTransient<ScoreService>();
        services.AddTransient<ScoreTypeService>();
        services.AddTransient<SemesterService>();
        services.AddTransient<StudentService>();
        services.AddTransient<SubjectService>();
        services.AddTransient<ExerciseService>();
        services.AddTransient<UserService>();
        services.AddTransient<SubjectTeacherService>();
        services.AddTransient<MoodleIntegrationService>();
        services.AddTransient<TokenGeneratorService>();

        return services;
    }

    public static IServiceCollection AddMassTransitWithConsumers(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.AddConsumer<AssignmentAcceptedConsumer>();
            x.AddConsumer<PullRequestOpenedConsumer>();
            x.AddConsumer<TeacherAssignedConsumer>();
            x.AddConsumer<AssignmentGradedByTeacherConsumer>();
            x.AddConsumer<PullRequestStatusChangedConsumer>();
            x.AddConsumer<CiEvaluationCompletedConsumer>();

            x.UsingAzureServiceBus((context, cfg) =>
            {
                var configuration = context.GetRequiredService<IConfiguration>();
                var connectionString = configuration.GetConnectionString("ahk-events");

                cfg.Host(connectionString);
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
