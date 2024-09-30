using GradeManagement.Bll.Services;
using GradeManagement.Server.Authorization.Policies;
using GradeManagement.Server.Controllers.BaseControllers;
using GradeManagement.Shared.Dtos;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradeManagement.Server.Controllers;

[Authorize]
[Route("api/assignments")]
[ApiController]
public class AssignmentController(AssignmentService assignmentService)
    : QueryControllerBase<Assignment>(assignmentService)
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<IEnumerable<Assignment>> GetAllAsync() => await base.GetAllAsync();

    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public override async Task<Assignment> GetByIdAsync(long id) => await base.GetByIdAsync(id);

    [HttpGet("{id:long}/pullrequests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Authorize(Policy = DemonstratorOnSubjectRequirement.PolicyName)]
    public async Task<IEnumerable<PullRequest>> GetAllPullRequestsByIdAsync([FromRoute] long id)
    {
        return await assignmentService.GetAllPullRequestsByIdAsync(id);
    }
}
