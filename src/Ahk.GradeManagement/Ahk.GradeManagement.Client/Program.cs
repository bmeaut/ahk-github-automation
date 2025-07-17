using Ahk.GradeManagement.Client.Network;
using Ahk.GradeManagement.Client.Policies;
using Ahk.GradeManagement.Client.Services;
using Ahk.GradeManagement.Shared.Enums;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using MudExtensions.Services;

namespace Ahk.GradeManagement.Client;

public class Program
{
    public const string ServerApi = "ServerAPI";

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddMsalAuthentication(options =>
        {
            builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
            options.ProviderOptions.DefaultAccessTokenScopes.Add("api://0ff49368-7e23-4e6a-9c57-973a6cac8bbd/AHK2.API");
            //Add scope for email
        });
        builder.Services.AddScoped<IAuthorizationHandler, UserTypeAuthorizationHandler>();
        builder.Services.AddAuthorizationCore(options =>
        {
            options.AddPolicy(Policy.RequireAdmin, policy => policy.Requirements.Add(new UserTypeRequirement([UserType.Admin])));
            options.AddPolicy(Policy.RequireTeacher, policy => policy.Requirements.Add(new UserTypeRequirement([UserType.Teacher, UserType.Admin])));
        });

        builder.Services.AddScoped<CrudSnackbarService>();
        builder.Services.AddScoped<SubjectService>();
        builder.Services.AddSingleton<SelectedSubjectService>();
        builder.Services.AddSingleton<CurrentUserService>();

        builder.Services.AddTransient<AuthorizationMessageHandler>();
        builder.Services.AddTransient<ExceptionMessageHandler>();
        builder.Services.AddTransient<SubjectHeaderHandler>();

        builder.Services.ConfigureHttpClientDefaults(b => b
            .AddHttpMessageHandler<AuthorizationMessageHandler>()
            .AddHttpMessageHandler<ExceptionMessageHandler>()
            .AddHttpMessageHandler<SubjectHeaderHandler>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)));

        builder.Services.AddHttpClient<SubjectClient>();
        builder.Services.AddHttpClient<CourseClient>();
        builder.Services.AddHttpClient<SemesterClient>();
        builder.Services.AddHttpClient<LanguageClient>();
        builder.Services.AddHttpClient<UserClient>();
        builder.Services.AddHttpClient<GroupClient>();
        builder.Services.AddHttpClient<ExerciseClient>();
        builder.Services.AddHttpClient<AssignmentClient>();
        builder.Services.AddHttpClient<StudentClient>();
        builder.Services.AddHttpClient<DashboardClient>();

        builder.Services.AddMudServices();
        builder.Services.AddMudExtensions();

        await builder.Build().RunAsync();
    }
}
