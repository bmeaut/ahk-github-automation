using GradeManagement.Bll;
using GradeManagement.Bll.Services;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync([FromRoute] long id)
    {
        return await pullRequestService.GetAllScoresByIdSortedByDateDescendingAsync(id);
    }
}
