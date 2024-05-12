using GradeManagement.Bll;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/pullrequests")]
[ApiController]
public class PullRequestController(PullRequestService pullRequestService) : ControllerBase
{
    [HttpGet("{id:long}/scores")]
    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync([FromRoute] long id)
    {
        return await pullRequestService.GetAllScoresByIdSortedByDateDescendingAsync(id);
    }
}
