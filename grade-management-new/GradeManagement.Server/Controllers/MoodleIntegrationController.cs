using GradeManagement.Bll.Services.Moodle;

using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/moodle")]
[ApiController]
public class MoodleIntegrationController(MoodleIntegrationService moodleIntegrationService) : ControllerBase
{

    [HttpPost]
    [Route("oidc")]
    public IActionResult Oidc()
    {
        var formData = Request.Form;
        var url = moodleIntegrationService.HandleOidc(formData);
        return Redirect(url);
    }

    [HttpPost]
    public async Task<IActionResult> CheckPlatformAnswerAsync()
    {
        var form = Request.Form;
        var url = await moodleIntegrationService.HandleJWT(form);
        return Redirect(url);
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok();
    }
}
