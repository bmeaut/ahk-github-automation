using Ahk.GradeManagement.Client.Auth;
using Ahk.GradeManagement.Client.Network;
using Ahk.GradeManagement.Client.Services;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using MudBlazor.Services;

using MudExtensions.Services;

namespace Ahk.GradeManagement.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddGradeManagementAuthentication(builder.Configuration);

        builder.Services.AddGradeManagementHttpClients(builder.HostEnvironment.BaseAddress);

        builder.Services.AddScoped<CrudSnackbarService>();
        builder.Services.AddScoped<SubjectService>();
        builder.Services.AddSingleton<SelectedSubjectService>();
        builder.Services.AddSingleton<CurrentUserService>();

        builder.Services.AddMudServices();
        builder.Services.AddMudExtensions();

        await builder.Build().RunAsync();
    }
}
