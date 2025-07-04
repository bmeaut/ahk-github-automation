using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController : ControllerBase
{
    [HttpGet("app-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAppUrl()
    {
        var appUrl = Environment.GetEnvironmentVariable("APP_URL");

        if (string.IsNullOrEmpty(appUrl))
        {
            return NotFound("APP_URL is not set.");
        }

        return Ok(appUrl);
    }

    [HttpGet("monitor-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetMonitorUrl()
    {
        var monitorUrl = Environment.GetEnvironmentVariable("MONITOR_URL");

        if (string.IsNullOrEmpty(monitorUrl))
        {
            return NotFound("MONITOR_URL is not set.");
        }

        return Ok(monitorUrl);
    }
}
