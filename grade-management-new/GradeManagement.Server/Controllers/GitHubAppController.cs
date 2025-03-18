using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Route("api/github")]
[ApiController]
public class GitHubAppController
{
    [HttpGet]
    public async Task CreateGitHubApp([FromQuery] string code)
    {
        Console.WriteLine(code);
    }

}
