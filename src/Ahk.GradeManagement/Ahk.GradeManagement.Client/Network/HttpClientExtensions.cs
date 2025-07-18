namespace Ahk.GradeManagement.Client.Network;

public static class HttpClientExtensions
{
    public static IServiceCollection AddGradeManagementHttpClients(this IServiceCollection services, string baseAddress)
    {
        services.AddTransient<AuthorizationMessageHandler>();
        services.AddTransient<ExceptionMessageHandler>();
        services.AddTransient<SubjectHeaderHandler>();

        services.ConfigureHttpClientDefaults(b => b
            .AddHttpMessageHandler<AuthorizationMessageHandler>()
            .AddHttpMessageHandler<ExceptionMessageHandler>()
            .AddHttpMessageHandler<SubjectHeaderHandler>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseAddress)));

        services.AddHttpClient<SubjectClient>();
        services.AddHttpClient<CourseClient>();
        services.AddHttpClient<SemesterClient>();
        services.AddHttpClient<LanguageClient>();
        services.AddHttpClient<UserClient>();
        services.AddHttpClient<GroupClient>();
        services.AddHttpClient<ExerciseClient>();
        services.AddHttpClient<AssignmentClient>();
        services.AddHttpClient<StudentClient>();
        services.AddHttpClient<DashboardClient>();

        return services;
    }
}
