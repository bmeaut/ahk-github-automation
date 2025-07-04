using Ahk.GradeManagement.Api.Authorization.Policies;
using Ahk.GradeManagement.Bll.Services;
using Ahk.GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ahk.GradeManagement.Api.Controllers;

[Authorize]
[Route("api/pullrequests")]
[ApiController]
public class PullRequestController(PullRequestService pullRequestService) : ControllerBase
{
    [HttpGet("{id:long}/scores")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<Score>> GetAllScoresByIdAsync([FromRoute] long id)
    {
        return await pullRequestService.GetAllScoresByIdSortedByDateDescendingAsync(id);
    }
}
