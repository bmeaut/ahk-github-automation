using Ahk.GradeManagement.Backend.Common.Options;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Ahk.GradeManagement.Api.Controllers;

[ApiController]
[Route("api/config")]
public class ConfigController(IOptions<AhkOptions> ahkOptionsAccessor) : ControllerBase
{
    private readonly AhkOptions _ahkOptions = ahkOptionsAccessor.Value;

    [HttpGet("app-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAppUrl()
    {
        return Ok(_ahkOptions.AppUrl);
    }

    [HttpGet("monitor-url")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetMonitorUrl()
    {
       return Ok(_ahkOptions.MonitorUrl);
    }
}
