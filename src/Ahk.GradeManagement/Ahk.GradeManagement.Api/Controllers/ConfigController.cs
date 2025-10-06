using Ahk.GradeManagement.Backend.Common.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.ServiceDiscovery;

namespace Ahk.GradeManagement.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController(ServiceEndpointResolver serviceEndpointResolver) : ControllerBase
{
    [HttpGet("app-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppUrl()
    {
        return Ok((await GetUriAsync("http+https://ahk-grademanagement-api")).ToString());
    }

    [HttpGet("monitor-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMonitorUrl()
    {
        return Ok((await GetUriAsync("http+https://ahk-github-monitor")).ToString());
    }

    private async Task<Uri> GetUriAsync(string serviceName, string path = "")
    {
        var serviceEndpoint = await serviceEndpointResolver.GetEndpointsAsync(serviceName, CancellationToken.None);
        return new Uri(new Uri(serviceEndpoint.Endpoints[0].EndPoint.ToString()!), relativeUri: path);
    }
}
