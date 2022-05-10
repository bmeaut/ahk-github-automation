using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Ahk.Lifecycle.Management.Ui;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddHttpClient("lifecycle-management", httpClient =>
{
    httpClient.BaseAddress = new Uri(builder.Configuration.GetSection("baseAddress").Value);
});
builder.Services.AddMudServices();

await builder.Build().RunAsync();
