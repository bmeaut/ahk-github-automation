using GradeManagement.Client.Network;
using GradeManagement.Client.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using MudExtensions.Services;

using AuthorizationMessageHandler = GradeManagement.Client.Network.AuthorizationMessageHandler;

namespace GradeManagement.Client
{
    public class Program
    {
        public const string ServerApi = "ServerAPI";
        public const string SubjectApi = "SubjectAPI";

        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });

            builder.Services.AddMsalAuthentication(options =>
            {
                builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
                options.ProviderOptions.DefaultAccessTokenScopes
                    .Add("api://01834b53-87a0-4236-85d3-a999ecec0115/access_backend");
                //Add scope for email
            });
            // builder.Services.AddAuthorizationCore(options =>
            //     options.AddPolicy(TeacherRequirement.PolicyName,
            //         policy => policy.Requirements.Add(new TeacherRequirement())));
            // builder.Services.AddSingleton<IAuthorizationHandler, TeacherRequirementHandler>();

            builder.Services.AddScoped<SubjectService>();
            builder.Services.AddScoped<CrudSnackbarService>();

            builder.Services.AddTransient(sp =>
                new AuthorizationMessageHandler(sp.GetRequiredService<IAccessTokenProvider>()));
            builder.Services.AddTransient(sp =>
                new ExceptionMessageHandler(sp.GetRequiredService<CrudSnackbarService>()));

            // Registering HttpClient that uses our custom handler
            builder.Services.AddHttpClient(SubjectApi,
                    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<ExceptionMessageHandler>()
                .AddHttpMessageHandler<AuthorizationMessageHandler>();

            builder.Services.AddScoped<SubjectClient>(sp =>
                new SubjectClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(SubjectApi)));

            builder.Services.AddTransient(sp => new SubjectHeaderHandler(sp.GetRequiredService<SubjectService>()));

            // Registering HttpClient that uses our custom handler
            builder.Services.AddHttpClient(ServerApi,
                    client => client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
                .AddHttpMessageHandler<ExceptionMessageHandler>()
                .AddHttpMessageHandler<AuthorizationMessageHandler>()
                .AddHttpMessageHandler<SubjectHeaderHandler>();


            builder.Services.AddScoped<CourseClient>(sp =>
                new CourseClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<SemesterClient>(sp =>
                new SemesterClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<LanguageClient>(sp =>
                new LanguageClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<UserClient>(sp =>
                new UserClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<GroupClient>(sp =>
                new GroupClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<ExerciseClient>(sp =>
                new ExerciseClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<AssignmentClient>(sp =>
                new AssignmentClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<StudentClient>(sp =>
                new StudentClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));
            builder.Services.AddScoped<DashboardClient>(sp =>
                new DashboardClient(sp.GetRequiredService<IHttpClientFactory>().CreateClient(ServerApi)));


            builder.Services.AddMudServices();
            builder.Services.AddMudExtensions();


            await builder.Build().RunAsync();
        }
    }
}
